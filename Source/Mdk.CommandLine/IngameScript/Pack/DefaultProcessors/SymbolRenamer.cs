using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        
        string GetMinifiedName(string oldName)
        {
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
        
        public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            var newNode = (VariableDeclaratorSyntax?)base.VisitVariableDeclarator(node);
            if (!TryGetSymbol(newNode, out _))
                return newNode;
            var oldName = newNode!.Identifier.Text;
            var newName = GetMinifiedName(oldName);
            // Create a new identifier, preserving all trivia of the old one
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
        
        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            var newNode = (IdentifierNameSyntax?)base.VisitIdentifierName(node);
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
            return overrideDefinitionNode == null || overrideDefinitionNode.ShouldBePreserved();
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
            /* Confusingly, the code below breaks the minification and I have no idea why.
               So let's just leave `var` alone for now.
            else if (node is VariableDeclarationSyntax { Type.IsVar: true } vds)
            {
                // Find the inferred type symbol
                var typeInfo = _semanticModel.GetTypeInfo(vds.Type);
                var typeNodeId = vds.Type.GetAnnotations("NodeID").FirstOrDefault()?.Data;
                if (typeInfo.Type != null && typeNodeId != null)
                    _symbolMap[typeNodeId] = typeInfo.Type;
            }
            */
        }
    }
    
    class UsageScanWalker(SemanticModel semanticModel, Dictionary<string, ISymbol> symbolMap) : BaseWalker
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
            var symbolInfo = _semanticModel.GetSymbolInfo(node);
            var symbol = symbolInfo.Symbol;
            if (symbol is IMethodSymbol { MethodKind: MethodKind.ReducedExtension } methodSymbol)
                symbol = methodSymbol.ReducedFrom ?? symbol;
            
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