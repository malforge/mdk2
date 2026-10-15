using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

/// <summary>
///     A processor that renames symbols in the script to single or double character names, in order to reduce the script's
///     size.
/// </summary>
[RunAfter<CommentStripper>]
public partial class SymbolRenamer : IDocumentProcessor
{
    /// <inheritdoc />
    public async Task<Document> ProcessAsync(Document document, IPackContext context)
    {
        if (context.Parameters.PackVerb.MinifierLevel < MinifierLevel.Full)
        {
            context.Console.Trace("Skipping symbol renaming because the minifier level < Full.");
            return document;
        }

        context.Console.Trace("Starting symbol renaming...");
        var stopwatch = Stopwatch.StartNew();
        var syntaxRoot = await document.GetSyntaxRootAsync() ?? throw new InvalidOperationException("Failed to get syntax root.");
        var identifierRewriter = new NodeIdentifierRewriter();
        syntaxRoot = identifierRewriter.Visit(syntaxRoot) ?? throw new InvalidOperationException("Failed to identify nodes.");
        document = document.WithSyntaxRoot(syntaxRoot);
        var semanticModel = await document.GetSemanticModelAsync() ?? throw new InvalidOperationException("Failed to get semantic model.");
        syntaxRoot = await document.GetSyntaxRootAsync() ?? throw new InvalidOperationException("Failed to get syntax root.");

        var idMap = new Dictionary<string, ISymbol>(StringComparer.Ordinal);
        var scanPhase1 = new DefinitionScanWalker(semanticModel, idMap, context);
        scanPhase1.Visit(syntaxRoot);
        var scanPhase2 = new UsageScanWalker(semanticModel, idMap);
        scanPhase2.Visit(syntaxRoot);

        var renamer = new SymbolRenamingRewriter(idMap, semanticModel, CollectExistingNames(syntaxRoot));
        syntaxRoot = renamer.Visit(syntaxRoot) ?? throw new InvalidOperationException("Failed to rename symbols.");

        document = document.WithSyntaxRoot(syntaxRoot);

        context.Console.Trace("Symbol renaming complete in " + stopwatch.Elapsed);

        if (context.Console.TraceEnabled)
        {
            var fileName = Path.Combine(context.FileSystem.TraceDirectory, "symbol.map");
            context.Console.Trace($"Writing intermediate symbol map to {fileName}");
            await context.FileSystem.WriteTraceAsync(fileName,
                "[symbolmap]\n" +
                string.Join("\n", renamer.NameMap.Select(kvp => $"{kvp.Value}={kvp.Key}")));
        }
        // if (!context.Parameters.PackVerb.NoSymbolMap)
        //     context.FileSystem.Write("symbol.map", string.Join("\n", idMap.Select(kvp => $"{kvp.Value}={kvp.Key}")));
        return document;
    }

    /// <summary>
    ///     Collects every identifier name the script already uses. A generated name must not be one of them: some
    ///     identifiers survive the renaming - preserved declarations, members pinned because a preserved member
    ///     overrides or implements them, and everything declared outside the script - and handing one of those out
    ///     again creates a duplicate or shadowing declaration. There is no shortage of generated names, so this is
    ///     deliberately blunt rather than trying to predict which identifiers survive.
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    static HashSet<string> CollectExistingNames(SyntaxNode root)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in root.DescendantTokens())
        {
            if (token.IsKind(SyntaxKind.IdentifierToken))
                names.Add(token.ValueText);
        }

        return names;
    }

    /// <summary>
    ///     Changes the given integer into a string representing it in Base-N
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <param name="baseChars">The characters the Base-N number system consists of</param>
    /// <returns></returns>
    static string ToNBaseString(int value, char[] baseChars)
    {
        // 32 is the worst case buffer size for base 2 and int.MaxValue
        var i = 32;
        var buffer = new char[i];
        var targetBase = baseChars.Length;

        do
        {
            buffer[--i] = baseChars[value % targetBase];
            value /= targetBase;
        } while (value > 0);

        var result = new char[32 - i];
        Array.Copy(buffer, i, result, 0, 32 - i);

        return new string(result);
    }

    class NodeIdentifierRewriter : CSharpSyntaxRewriter
    {
        long _identSrc;

        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            if (node == null)
                return null;
            return base.Visit(node).WithAdditionalAnnotations(new SyntaxAnnotation("NodeID", $"ID_{_identSrc++}"));
        }
    }

    class SymbolRenamingRewriter(Dictionary<string, ISymbol> symbolMap, SemanticModel semanticModel, HashSet<string> existingNames) : CSharpSyntaxRewriter
    {
        readonly HashSet<string> _distinctSymbolNames = existingNames;
        readonly Dictionary<string, string> _nameMap = new(StringComparer.Ordinal);
        readonly Dictionary<string, ISymbol> _symbolMap = symbolMap;
        readonly SemanticModel _semanticModel = semanticModel;
        int _symbolSrc;

        public IReadOnlyDictionary<string, string> NameMap => _nameMap;

        static bool IsPreserved(EnumDeclarationSyntax node) => node.ShouldBePreserved() || node.Identifier.ShouldBePreserved();
        static bool IsPreserved(EnumMemberDeclarationSyntax node) => node.ShouldBePreserved() || node.Identifier.ShouldBePreserved();
        
        string GetMinifiedName(string oldName)
        {
            if (oldName.StartsWith('@'))
                oldName = oldName.Substring(1);
            if (_nameMap.TryGetValue(oldName, out var newName))
                return newName;
            string name;
            do
                name = ToNBaseString(_symbolSrc++, BaseNChars);
            while (_distinctSymbolNames.Contains(name));
            _nameMap[oldName] = name;
            _distinctSymbolNames.Add(name);
            return name;
        }

        bool TryGetSymbol(SyntaxNode? node, [MaybeNullWhen(false)] out ISymbol symbol)
        {
            if (node == null)
            {
                symbol = default;
                return false;
            }
            var id = node.GetAnnotations("NodeID").FirstOrDefault()?.Data;
            if (id == null)
            {
                symbol = default;
                return false;
            }
            if (!_symbolMap.TryGetValue(id, out symbol))
            {
                symbol = default;
                return false;
            }
            return true;
        }

        public override SyntaxNode? VisitLabeledStatement(LabeledStatementSyntax node)
        {
            var newNode = (LabeledStatementSyntax?)base.VisitLabeledStatement(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = newNode!.ShouldBePreserved();
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var newNode = (ConstructorDeclarationSyntax?)base.VisitConstructorDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = newNode!.ShouldBePreserved();
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitTypeParameter(TypeParameterSyntax node)
        {
            var newNode = (TypeParameterSyntax?)base.VisitTypeParameter(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = newNode!.ShouldBePreserved();
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitCatchDeclaration(CatchDeclarationSyntax node)
        {
            var newNode = (CatchDeclarationSyntax?)base.VisitCatchDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = newNode!.ShouldBePreserved();
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            var newNode = (VariableDeclarationSyntax?)base.VisitVariableDeclaration(node);
            var shouldBePreserved = newNode!.ShouldBePreserved();
            
            // If the type is a qualified name (containing a dot) replace it with a `var` type
            // as it's likely to be either as short, or shorter than the original type name.
            if (newNode?.Type is QualifiedNameSyntax qualifiedName)
            {
                if (IsVarAllowedFor(qualifiedName))
                {
                    var varType = SyntaxFactory.IdentifierName("var")
                        .WithLeadingTrivia(qualifiedName.GetLeadingTrivia())
                        .WithTrailingTrivia(qualifiedName.GetTrailingTrivia());
                    if (shouldBePreserved)
                        varType = varType.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
                    return newNode.WithType(varType);
                }
            }

            return newNode!;
        }

        public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            var newNode = (VariableDeclaratorSyntax?)base.VisitVariableDeclarator(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = newNode!.ShouldBePreserved();
            
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitParameter(ParameterSyntax node)
        {
            var newNode = (ParameterSyntax?)base.VisitParameter(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = newNode!.ShouldBePreserved();

            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }


        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var newNode = (MethodDeclarationSyntax?)base.VisitMethodDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = newNode!.ShouldBePreserved();
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var newNode = (ClassDeclarationSyntax?)base.VisitClassDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = newNode!.ShouldBePreserved();
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
        {
            var newNode = (StructDeclarationSyntax?)base.VisitStructDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = newNode!.ShouldBePreserved();
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            var newNode = (InterfaceDeclarationSyntax?)base.VisitInterfaceDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = newNode!.ShouldBePreserved();
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            var newNode = (EnumDeclarationSyntax?)base.VisitEnumDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = IsPreserved(newNode!);
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
        {
            var newNode = (EnumMemberDeclarationSyntax?)base.VisitEnumMemberDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = IsPreserved(newNode!);
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var newNode = (PropertyDeclarationSyntax?)base.VisitPropertyDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = newNode!.ShouldBePreserved();
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitEventDeclaration(EventDeclarationSyntax node)
        {
            var newNode = (EventDeclarationSyntax?)base.VisitEventDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = newNode!.ShouldBePreserved();
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            var newNode = (DelegateDeclarationSyntax?)base.VisitDelegateDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = newNode!.ShouldBePreserved();
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
        {
            var newNode = (ForEachStatementSyntax?)base.VisitForEachStatement(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = newNode!.ShouldBePreserved();

            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitFromClause(FromClauseSyntax node)
        {
            var newNode = (FromClauseSyntax?)base.VisitFromClause(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = newNode!.Identifier.ShouldBePreserved();
            var oldName = newNode.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitLetClause(LetClauseSyntax node)
        {
            var newNode = (LetClauseSyntax?)base.VisitLetClause(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = newNode!.Identifier.ShouldBePreserved();
            var oldName = newNode.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitJoinClause(JoinClauseSyntax node)
        {
            var newNode = (JoinClauseSyntax?)base.VisitJoinClause(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = newNode!.Identifier.ShouldBePreserved();
            var oldName = newNode.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitJoinIntoClause(JoinIntoClauseSyntax node)
        {
            var newNode = (JoinIntoClauseSyntax?)base.VisitJoinIntoClause(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = newNode!.Identifier.ShouldBePreserved();
            var oldName = newNode.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitQueryContinuation(QueryContinuationSyntax node)
        {
            var newNode = (QueryContinuationSyntax?)base.VisitQueryContinuation(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var shouldBePreserved = newNode!.Identifier.ShouldBePreserved();
            var oldName = newNode.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node)
        {
            var shouldBePreserved = node!.ShouldBePreserved();
            if (IsVarAllowedFor(node))
            {
                var varNode = SyntaxFactory.IdentifierName("var")
                    .WithLeadingTrivia(node.GetLeadingTrivia())
                    .WithTrailingTrivia(node.GetTrailingTrivia());
                if (shouldBePreserved)
                    varNode = varNode.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
                return varNode;
            }
            
            return base.VisitQualifiedName(node);
        }

    private static bool IsVarAllowedFor(QualifiedNameSyntax node)
    {
        var parent = node.Parent;

        switch (parent)
        {
            case VariableDeclarationSyntax variableDeclaration:
                switch (variableDeclaration.Parent)
                {
                    case LocalDeclarationStatementSyntax:
                    {
                        foreach (var variable in variableDeclaration.Variables)
                        {
                            if (variable.Initializer == null)
                                return false;
                        }
                        return true;
                    }
                    case ForStatementSyntax:
                    {
                        foreach (var variable in variableDeclaration.Variables)
                        {
                            if (variable.Initializer == null)
                                return false;
                        }
                        return true;
                    }
                }
                break;
            
            case ForEachStatementSyntax or ForEachVariableStatementSyntax:
                return true;
            
            case UsingStatementSyntax { Declaration: not null } usingStatement:
            {
                foreach (var variable in usingStatement.Declaration.Variables)
                {
                    if (variable.Initializer == null)
                        return false;
                }
                return true;
            }
        }

        return false;
    }
    
        // bool TryProcessType(TypeSyntax type, bool canUseVar, out TypeSyntax final)
        // {
        //     if (type is QualifiedNameSyntax qualifiedName)
        //     {
        //         // If we can use `var` as the type, replace the qualified name with `var`
        //         if (canUseVar)
        //         {
        //             final = SyntaxFactory.IdentifierName("var")
        //                 .WithLeadingTrivia(qualifiedName.GetLeadingTrivia())
        //                 .WithTrailingTrivia(qualifiedName.GetTrailingTrivia());
        //             return true;
        //         }
        //         // Otherwise, we need to replace every part of the qualified name with a minified name
        //         var newLeft = TryProcessTypeName(qualifiedName.Left, out var left) ? left : qualifiedName.Left;
        //         var newRight = TryProcessTypeName(qualifiedName.Right, out var right) ? right : qualifiedName.Right;
        //         final = SyntaxFactory.QualifiedName(newLeft, (SimpleNameSyntax)newRight)
        //             .WithLeadingTrivia(qualifiedName.GetLeadingTrivia())
        //             .WithTrailingTrivia(qualifiedName.GetTrailingTrivia());
        //         return true;
        //     }
        //         
        //     
        // }

        // bool TryProcessTypeName(NameSyntax name, out NameSyntax final)
        // {
        //     if (name is IdentifierNameSyntax identifierName)
        //     {
        //         if (TryGetSymbol(identifierName, out var symbol))
        //         {
        //             var oldName = identifierName.Identifier.Text;
        //             var newName = GetMinifiedName(oldName);
        //             var newIdentifier = SyntaxFactory.Identifier(newName)
        //                 .WithLeadingTrivia(identifierName.Identifier.LeadingTrivia)
        //                 .WithTrailingTrivia(identifierName.Identifier.TrailingTrivia);
        //             final = SyntaxFactory.IdentifierName(newIdentifier);
        //             return true;
        //         }
        //     }
        //
        //     final = name;
        //     return false;
        // }

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            var newNode = (IdentifierNameSyntax?)base.VisitIdentifierName(node);
            var shouldBePreserved = newNode!.ShouldBePreserved();

            // If this is a `var` identifier, don't rename it
            if (newNode?.Identifier.Text == "var" && newNode.Parent is VariableDeclarationSyntax or ForEachStatementSyntax)
                return newNode;
            
            if (!TryGetSymbol(newNode, out var symbol))
                return newNode;
            var oldName = symbol.Name;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode!.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }
        
        public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
        {
            var newNode = (GenericNameSyntax?)base.VisitGenericName(node);
            if (!TryGetSymbol(newNode, out var symbol))
                return newNode;
            var shouldBePreserved = newNode!.ShouldBePreserved();
            var oldName = symbol.Name;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode!.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            if (shouldBePreserved)
                newIdentifier = newIdentifier.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"));
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            // Special handling for nameof() expressions
            // The identifier inside nameof() is not treated as a normal expression by Roslyn,
            // so we need to manually handle renaming it
            if (node is { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" }, ArgumentList.Arguments.Count: > 0 })
            {
                var arg = node.ArgumentList.Arguments[0];
                if (arg.Expression is IdentifierNameSyntax identifierArg)
                {
                    // Use the semantic model to get the symbol for the identifier
                    var symbolInfo = _semanticModel.GetSymbolInfo(identifierArg);
                    var symbol = symbolInfo.Symbol?.OriginalDefinition ?? symbolInfo.Symbol;
                        
                    // If no single symbol found, check candidate symbols
                    // This happens when there's ambiguity (e.g., method group with same name as type)
                    if (symbol == null && symbolInfo.CandidateSymbols.Length > 0)
                    {
                        // Try to find any candidate that we're tracking for renaming
                        symbol = symbolInfo.CandidateSymbols.FirstOrDefault(s => 
                            _symbolMap.Values.Contains(s.OriginalDefinition ?? s, SymbolEqualityComparer.Default));
                    }
                        
                    if (symbol != null)
                    {
                        var originalSymbol = symbol.OriginalDefinition ?? symbol;
                        var inMap = _symbolMap.Values.Contains(originalSymbol, SymbolEqualityComparer.Default);
                        if (inMap)
                        {
                            var oldName = symbol.Name;
                            var newName = GetMinifiedName(oldName);
                            var newIdentifier = SyntaxFactory.Identifier(newName)
                                .WithLeadingTrivia(identifierArg.Identifier.LeadingTrivia)
                                .WithTrailingTrivia(identifierArg.Identifier.TrailingTrivia);
                            var newIdentifierName = identifierArg.WithIdentifier(newIdentifier);
                            var newArg = arg.WithExpression(newIdentifierName);
                            var newArgList = node.ArgumentList.WithArguments(
                                node.ArgumentList.Arguments.Replace(arg, newArg));
                            return node.WithArgumentList(newArgList);
                        }
                    }
                }
            }
            
            return (InvocationExpressionSyntax?)base.VisitInvocationExpression(node);
        }
    }

    abstract class BaseWalker : CSharpSyntaxWalker
    {
        static T? GetOverriddenSymbol<T>(T symbol) where T : class, ISymbol
        {
            if (symbol.IsOverride)
            {
                return symbol switch
                {
                    IMethodSymbol methodSymbol => methodSymbol.OverriddenMethod as T,
                    IPropertySymbol propertySymbol => propertySymbol.OverriddenProperty as T,
                    IEventSymbol eventSymbol => eventSymbol.OverriddenEvent as T,
                    _ => null
                };
            }

            if (symbol.TryGetInterfaceImplementation(out var interfaceMethod))
                return interfaceMethod as T;

            return null;
        }

        protected static bool IsOverriddenSymbolPreserved<T>(T symbol) where T : class, ISymbol
        {
            var overrideBase = GetOverriddenSymbol(symbol);
            if (overrideBase == null)
                return false;
            var overrideDefinitionNode = overrideBase.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            return overrideDefinitionNode == null || overrideDefinitionNode.ShouldBePreserved() || IsOverriddenSymbolPreserved(overrideBase);
        }

        protected static bool IsReferencedSymbolPreserved<T>(T symbol) where T : class, ISymbol
        {
            var references = symbol.DeclaringSyntaxReferences;
            if (references.Length == 0)
                return false;
            return references.All(r => r.GetSyntax().ShouldBePreserved());
        }

        SemanticModel? _pinnedModel;
        HashSet<ISymbol>? _pinnedByPreservedOverride;

        /// <summary>
        ///     Determines whether the given symbol belongs to an override or interface implementation family which
        ///     contains a preserved member. Every member of such a family carries the same name, and the preserved
        ///     one cannot change, so none of them can be renamed: an override would override nothing (CS0115) and an
        ///     implementation would no longer implement its interface member (CS0535).
        /// </summary>
        /// <remarks>
        ///     <see cref="IsOverriddenSymbolPreserved{T}" /> only looks from a symbol up to what it overrides.
        ///     Nothing on <see cref="ISymbol" /> answers "who overrides me", so the declarations in the script are
        ///     walked once and the result cached.
        /// </remarks>
        /// <param name="symbol"></param>
        /// <param name="semanticModel"></param>
        /// <returns></returns>
        protected bool IsOverridingSymbolPreserved(ISymbol symbol, SemanticModel semanticModel)
        {
            if (!ReferenceEquals(_pinnedModel, semanticModel))
            {
                _pinnedByPreservedOverride = CollectPinnedByPreservedOverride(semanticModel);
                _pinnedModel = semanticModel;
            }

            return _pinnedByPreservedOverride!.Contains(symbol.OriginalDefinition);
        }

        /// <summary>
        ///     Collects every member which has to keep its name because a preserved member shares its override or
        ///     interface implementation family.
        /// </summary>
        /// <param name="semanticModel"></param>
        /// <returns></returns>
        static HashSet<ISymbol> CollectPinnedByPreservedOverride(SemanticModel semanticModel)
        {
            var pinned = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            var root = semanticModel.SyntaxTree.GetRoot();
            // Without a single preserved declaration nothing can be pinned, and resolving the interfaces below is
            // not free.
            if (!root.DescendantNodes().Any(node => node.ShouldBePreserved()))
                return pinned;

            var family = new Dictionary<ISymbol, List<ISymbol>>(SymbolEqualityComparer.Default);
            var queue = new Queue<ISymbol>();

            foreach (var node in root.DescendantNodes())
            {
                switch (node)
                {
                    // Fields and event fields declare their symbols on the variable declarators, not on the
                    // declaration itself. An event field can implement an interface event.
                    case BaseFieldDeclarationSyntax fieldDeclaration:
                        foreach (var declarator in fieldDeclaration.Declaration.Variables)
                            Collect(declarator, fieldDeclaration.ShouldBePreserved() || declarator.ShouldBePreserved());
                        break;
                    case MemberDeclarationSyntax memberDeclaration:
                        Collect(memberDeclaration, memberDeclaration.ShouldBePreserved());
                        break;
                }
            }

            while (queue.Count > 0)
            {
                if (!family.TryGetValue(queue.Dequeue(), out var relatives))
                    continue;
                foreach (var relative in relatives)
                {
                    if (pinned.Add(relative))
                        queue.Enqueue(relative);
                }
            }

            return pinned;

            void Collect(SyntaxNode declaration, bool isPreserved)
            {
                if (semanticModel.GetDeclaredSymbol(declaration) is not { } declared)
                    return;
                var symbol = declared.OriginalDefinition;
                if (isPreserved && pinned.Add(symbol))
                    queue.Enqueue(symbol);

                if (symbol is INamedTypeSymbol type)
                {
                    // The member implementing an interface member can be inherited from a base type, so the pair has
                    // to be resolved from the implementing type rather than from the member's own containing type.
                    foreach (var interfaceMember in type.AllInterfaces.SelectMany(i => i.GetMembers()))
                    {
                        if (type.FindImplementationForInterfaceMember(interfaceMember) is { } implementation)
                            Relate(family, implementation.OriginalDefinition, interfaceMember.OriginalDefinition);
                    }

                    return;
                }

                if (GetOverriddenMember(symbol) is { } overridden)
                    Relate(family, symbol, overridden.OriginalDefinition);
            }

            static void Relate(Dictionary<ISymbol, List<ISymbol>> family, ISymbol first, ISymbol second)
            {
                Add(family, first, second);
                Add(family, second, first);

                static void Add(Dictionary<ISymbol, List<ISymbol>> family, ISymbol key, ISymbol value)
                {
                    if (!family.TryGetValue(key, out var relatives))
                        family[key] = relatives = new List<ISymbol>();
                    relatives.Add(value);
                }
            }
        }

        /// <summary>
        ///     Returns the member the given symbol overrides, if it is an override.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        static ISymbol? GetOverriddenMember(ISymbol symbol) => symbol switch
        {
            IMethodSymbol { OverriddenMethod: { } overriddenMethod } => overriddenMethod,
            IPropertySymbol { OverriddenProperty: { } overriddenProperty } => overriddenProperty,
            IEventSymbol { OverriddenEvent: { } overriddenEvent } => overriddenEvent,
            _ => null
        };
    }

    class DefinitionScanWalker(SemanticModel semanticModel, Dictionary<string, ISymbol> symbolMap, IPackContext context) : BaseWalker
    {
        readonly SemanticModel _semanticModel = semanticModel;
        readonly Dictionary<string, ISymbol> _symbolMap = symbolMap;
        readonly IPackContext _context = context;

        static bool ShouldBePreserved(SyntaxNode node) => node switch
        {
            EnumDeclarationSyntax enumDeclaration => enumDeclaration.ShouldBePreserved() || enumDeclaration.Identifier.ShouldBePreserved(),
            EnumMemberDeclarationSyntax enumMemberDeclaration => enumMemberDeclaration.ShouldBePreserved() || enumMemberDeclaration.Identifier.ShouldBePreserved(),
            _ => node.ShouldBePreserved()
        };

        public override void Visit(SyntaxNode? node)
        {
            base.Visit(node);
            if (node == null)
                return;

            if (ShouldBePreserved(node))
                return;
            var nodeId = node.GetAnnotations("NodeID").FirstOrDefault()?.Data;
            if (nodeId == null)
                return;
            var symbol = _semanticModel.GetDeclaredSymbol(node);

            if (symbol != null)
            {
                if (PreservedDeclarationRegistry.Contains(symbol, _context))
                    return;
                if (IsOverriddenSymbolPreserved(symbol))
                    return;
                if (IsOverridingSymbolPreserved(symbol, _semanticModel))
                    return;
                if (IsReferencedSymbolPreserved(symbol))
                    return;
                _symbolMap[nodeId] = symbol;
            }
        }
    }

    class UsageScanWalker(SemanticModel semanticModel, Dictionary<string, ISymbol> symbolMap) : BaseWalker
    {
        readonly SemanticModel _semanticModel = semanticModel;
        readonly Dictionary<string, ISymbol> _symbolMap = symbolMap;

        public override void Visit(SyntaxNode? node)
        {
            base.Visit(node);
            if (node is null)
                return;
            var nodeId = node.GetAnnotations("NodeID").FirstOrDefault()?.Data;
            if (nodeId == null)
                return;
            var symbolInfo = _semanticModel.GetSymbolInfo(node);
            var symbol = symbolInfo.Symbol?.OriginalDefinition ?? symbolInfo.Symbol;
            if (symbol is IMethodSymbol { MethodKind: MethodKind.ReducedExtension } methodSymbol)
                symbol = methodSymbol.ReducedFrom ?? symbol;
            
            switch (symbol)
            {
                // If the symbol is generic, we need to find the original definition
                case IMethodSymbol { Arity: > 0 } genericMethod:
                    symbol = genericMethod.OriginalDefinition;
                    break;
                case INamedTypeSymbol { Arity: > 0 } genericType:
                    symbol = genericType.OriginalDefinition;
                    break;
                
                // If the symbol is an indexer, it will be returning a _clone_ of the original indexer parameter definition.
                // So we need to find the original.
                case IParameterSymbol { OriginalDefinition.ContainingSymbol: IMethodSymbol { AssociatedSymbol: IPropertySymbol { IsIndexer: true } propertySymbol } } parameterSymbol:
                {
                    var originalParameterSymbol = propertySymbol.Parameters
                        .FirstOrDefault(p => p.Name == parameterSymbol.Name);

                    if (originalParameterSymbol != null)
                        symbol = originalParameterSymbol;
                    break;
                }
            }

            if (symbol != null && _symbolMap.Values.Contains(symbol, SymbolEqualityComparer.Default))
            {
                if (IsOverriddenSymbolPreserved(symbol))
                    return;
                if (IsReferencedSymbolPreserved(symbol))
                    return;
                _symbolMap[nodeId] = symbol;
            }

            // // Additional handling for invocation expressions
            // if (node is InvocationExpressionSyntax invocation)
            //     HandleInvocationExpression(invocation, nodeId);
        }


        // void HandleInvocationExpression(InvocationExpressionSyntax invocation, string nodeId)
        // {
        //     // Get the method symbol for the invocation
        //     var symbolInfo = _semanticModel.GetSymbolInfo(invocation);
        //     
        //     if (symbolInfo.Symbol is IMethodSymbol { MethodKind: MethodKind.ReducedExtension } methodSymbol)
        //     {
        //         // For extension methods, get the ReducedFrom property to find the original definition
        //         var originalMethod = methodSymbol.ReducedFrom ?? methodSymbol;
        //         if (_symbolMap.Values.Contains(originalMethod, SymbolEqualityComparer.Default))
        //             _symbolMap[nodeId] = originalMethod;
        //     }
        // }
    }
}
