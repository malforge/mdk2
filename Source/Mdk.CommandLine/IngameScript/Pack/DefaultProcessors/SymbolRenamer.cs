using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

/// <summary>
///     A processor that renames symbols in the script to single or double character names, in order to reduce the script's
///     size.
/// </summary>
[RunAfter<CommentStripper>]
public partial class SymbolRenamer : IScriptPostprocessor
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
        var scanPhase1 = new DefinitionScanWalker(semanticModel, idMap);
        scanPhase1.Visit(syntaxRoot);
        var scanPhase2 = new UsageScanWalker(semanticModel, idMap);
        scanPhase2.Visit(syntaxRoot);

        var renamer = new SymbolRenamingRewriter(idMap);
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

    class SymbolRenamingRewriter(Dictionary<string, ISymbol> symbolMap) : CSharpSyntaxRewriter
    {
        readonly HashSet<string> _distinctSymbolNames = new();
        readonly Dictionary<string, string> _nameMap = new(StringComparer.Ordinal);
        readonly Dictionary<string, ISymbol> _symbolMap = symbolMap;
        int _symbolSrc;

        public IReadOnlyDictionary<string, string> NameMap => _nameMap;
        
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
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var newNode = (ConstructorDeclarationSyntax?)base.VisitConstructorDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitTypeParameter(TypeParameterSyntax node)
        {
            var newNode = (TypeParameterSyntax?)base.VisitTypeParameter(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitCatchDeclaration(CatchDeclarationSyntax node)
        {
            var newNode = (CatchDeclarationSyntax?)base.VisitCatchDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            var newNode = (VariableDeclarationSyntax?)base.VisitVariableDeclaration(node);
            
            // If the type is a qualified name (containing a dot) replace it with a `var` type
            // as it's likely to be either as short, or shorter than the original type name.
            if (newNode?.Type is QualifiedNameSyntax qualifiedName)
            {
                if (IsVarAllowedFor(qualifiedName))
                {
                    var varType = SyntaxFactory.IdentifierName("var")
                        .WithLeadingTrivia(qualifiedName.GetLeadingTrivia())
                        .WithTrailingTrivia(qualifiedName.GetTrailingTrivia());
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
            
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitParameter(ParameterSyntax node)
        {
            var newNode = (ParameterSyntax?)base.VisitParameter(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;

            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            return newNode.WithIdentifier(newIdentifier);
        }


        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var newNode = (MethodDeclarationSyntax?)base.VisitMethodDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var newNode = (ClassDeclarationSyntax?)base.VisitClassDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
        {
            var newNode = (StructDeclarationSyntax?)base.VisitStructDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            var newNode = (InterfaceDeclarationSyntax?)base.VisitInterfaceDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            var newNode = (EnumDeclarationSyntax?)base.VisitEnumDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
        {
            var newNode = (EnumMemberDeclarationSyntax?)base.VisitEnumMemberDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var newNode = (PropertyDeclarationSyntax?)base.VisitPropertyDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitEventDeclaration(EventDeclarationSyntax node)
        {
            var newNode = (EventDeclarationSyntax?)base.VisitEventDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            var newNode = (DelegateDeclarationSyntax?)base.VisitDelegateDeclaration(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
        {
            var newNode = (ForEachStatementSyntax?)base.VisitForEachStatement(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;

            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node)
        {
            if (IsVarAllowedFor(node))
            {
                return SyntaxFactory.IdentifierName("var")
                    .WithLeadingTrivia(node.GetLeadingTrivia())
                    .WithTrailingTrivia(node.GetTrailingTrivia());
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
        
            // If this is a `var` identifier, don't rename it
            if (newNode?.Identifier.Text == "var" && (newNode.Parent is VariableDeclarationSyntax || newNode.Parent is ForEachStatementSyntax))
                return newNode;
            
            if (!TryGetSymbol(newNode, out var symbol))
                return newNode;
            var oldName = symbol.Name;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode!.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            return newNode.WithIdentifier(newIdentifier);
        }
        
        public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
        {
            var newNode = (GenericNameSyntax?)base.VisitGenericName(node);
            if (!TryGetSymbol(newNode, out var symbol))
                return newNode;
            var oldName = symbol.Name;
            var newName = GetMinifiedName(oldName);
            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithLeadingTrivia(newNode!.Identifier.LeadingTrivia)
                .WithTrailingTrivia(newNode.Identifier.TrailingTrivia);
            return newNode.WithIdentifier(newIdentifier);
        }

        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var newNode = (InvocationExpressionSyntax?)base.VisitInvocationExpression(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            return newNode;
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
    }

    class DefinitionScanWalker(SemanticModel semanticModel, Dictionary<string, ISymbol> symbolMap) : BaseWalker
    {
        readonly SemanticModel _semanticModel = semanticModel;
        readonly Dictionary<string, ISymbol> _symbolMap = symbolMap;

        public override void Visit(SyntaxNode? node)
        {
            base.Visit(node);
            if (node == null)
                return;

            if (node.ShouldBePreserved())
                return;
            var nodeId = node.GetAnnotations("NodeID").FirstOrDefault()?.Data;
            if (nodeId == null)
                return;
            var symbol = _semanticModel.GetDeclaredSymbol(node);

            if (symbol != null)
            {
                if (IsOverriddenSymbolPreserved(symbol))
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