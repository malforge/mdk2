﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk.CommandLine.Shared.Api;

/// <summary>
///     Extra utility functions while dealing with Roslyn code analysis
/// </summary>
public static class AnalysisExtensions
{
    /// <summary>
    ///     Determines whether the given symbol represents an interface implementation.
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    public static bool IsInterfaceImplementation(this ISymbol symbol)
    {
        if (symbol.ContainingType == null)
            return false;
        return symbol.ContainingType.AllInterfaces.SelectMany(i => i.GetMembers())
            .Any(member =>
            {
                var implementation = symbol.ContainingType.FindImplementationForInterfaceMember(member);
                return implementation != null && SymbolEqualityComparer.Default.Equals(implementation, symbol);
            });
    }
    
    /// <summary>
    /// Determines if the given symbol is an interface implementation and retrieves the interface member it implements if it is.
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public static bool TryGetInterfaceImplementation(this ISymbol symbol, [MaybeNullWhen(false)] out ISymbol result)
    { 
        if (symbol.ContainingType == null)
        {
            result = null;
            return false;
        }
        foreach (var i in symbol.ContainingType.AllInterfaces)
        {
            foreach (var member in i.GetMembers())
            {
                var implementation = symbol.ContainingType.FindImplementationForInterfaceMember(member);
                if (implementation != null && SymbolEqualityComparer.Default.Equals(implementation, symbol))
                {
                    result = member;
                    return true;
                }
            }
        }
        result = null;
        return false;
    }

    /// <summary>
    ///     Removes indentations from the given node if they are equal to or larger than the indicated number of indentations.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="node"></param>
    /// <param name="indentations"></param>
    /// <returns></returns>
    public static T Unindented<T>(this T node, int indentations) where T : SyntaxNode
    {
        var rewriter = new UnindentRewriter(indentations);
        return (T)rewriter.Visit(node);
    }

    /// <summary>
    ///     Retrieves the fully qualified name of the given symbol.
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    public static string GetFullName(this ISymbol symbol, DeclarationFullNameFlags flags = DeclarationFullNameFlags.Default)
    {
        if (symbol is INamedTypeSymbol namedType)
        {
            var declaratorSyntax = namedType
                .DeclaringSyntaxReferences
                .FirstOrDefault()
                ?.GetSyntax();
            if (declaratorSyntax is TypeDeclarationSyntax typeDeclaration)
                return typeDeclaration.GetFullName(flags)!;
        }
        var ident = new List<string>(10)
        {
            symbol.Name
        };
        var parent = symbol.ContainingSymbol;
        while (parent != null)
        {
            if (parent is INamespaceSymbol { IsGlobalNamespace: true })
                break;
            if ((flags & DeclarationFullNameFlags.WithoutNamespaceName) != 0 && parent is INamespaceSymbol)
                break;
            ident.Add(parent.Name);
            parent = parent.ContainingSymbol;
        }

        ident.Reverse();
        return string.Join(".", ident);
    }

    /// <summary>
    ///     Retrieves the fully qualified name of the given type declaration.
    /// </summary>
    /// <param name="declaration"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    public static string? GetFullName(this VariableDeclaratorSyntax? declaration, DeclarationFullNameFlags flags = DeclarationFullNameFlags.Default)
    {
        if (declaration == null)
            return null;

        var parent = declaration.Parent;
        while (!(parent is null || parent is TypeDeclarationSyntax))
            parent = parent.Parent;
        var parentName = GetFullName(parent as MemberDeclarationSyntax);
        if (parentName != null)
            return $"{parentName}.{declaration.Identifier}";

        return declaration.Identifier.ToString();
    }

    /// <summary>
    ///     Retrieves the fully qualified name of the given type declaration.
    /// </summary>
    /// <param name="declaration"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    public static string? GetFullName(this MemberDeclarationSyntax? declaration, DeclarationFullNameFlags flags = DeclarationFullNameFlags.Default)
    {
        switch (declaration)
        {
            case null:
                return null;
            case TypeDeclarationSyntax typeDeclaration:
                return GetFullNameCore(typeDeclaration, flags);
            case FieldDeclarationSyntax:
                throw new ArgumentException("Cannot get full names of field declarations as they may contain multiple fields. Retrieve the full names of the individual variable declarators.", nameof(declaration));
            case ConstructorDeclarationSyntax constructorDeclaration:
            {
                var parentName = GetFullName(constructorDeclaration.Parent as MemberDeclarationSyntax);
                if (parentName != null)
                    return $"{parentName}..ctor";
                return ".ctor";
            }
            case MethodDeclarationSyntax methodDeclaration:
            {
                var parentName = GetFullName(methodDeclaration.Parent as MemberDeclarationSyntax);
                if (parentName != null)
                    return $"{parentName}.{methodDeclaration.Identifier}";
                return methodDeclaration.Identifier.ToString();
            }
            case EventDeclarationSyntax eventDeclaration:
            {
                var parentName = GetFullName(eventDeclaration.Parent as MemberDeclarationSyntax);
                if (parentName != null)
                    return $"{parentName}.{eventDeclaration.Identifier}";
                return eventDeclaration.Identifier.ToString();
            }
            case DelegateDeclarationSyntax delegateDeclaration:
            {
                var parentName = GetFullName(delegateDeclaration.Parent as MemberDeclarationSyntax);
                if (parentName != null)
                    return $"{parentName}.{delegateDeclaration.Identifier}";
                return delegateDeclaration.Identifier.ToString();
            }
            default:
                throw new ArgumentException("Do not understand the declaration type", nameof(declaration));
        }
    }

    static string GetFullNameCore(TypeDeclarationSyntax typeDeclaration, DeclarationFullNameFlags flags)
    {
        var ident = new List<string>(10)
        {
            $"{typeDeclaration.Identifier}{typeDeclaration.TypeParameterList}"
        };
        var parent = typeDeclaration.Parent;
        while (parent != null)
        {
            if (parent is TypeDeclarationSyntax type)
            {
                ident.Add(type.Identifier.ToString());
                parent = parent.Parent;
                continue;
            }

            if ((flags & DeclarationFullNameFlags.WithoutNamespaceName) == 0 && parent is NamespaceDeclarationSyntax ns)
            {
                ident.Add(ns.Name.ToString());
                parent = parent.Parent;
                continue;
            }

            break;
        }

        ident.Reverse();
        return string.Join(".", ident);
    }

    /// <summary>
    ///     Determines whether the given syntax node is a symbol declaration.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="dump">Optional argument. If true, dumps the node kind and content to the debug console</param>
    /// <returns></returns>
    public static bool IsSymbolDeclaration(this SyntaxNode node, bool dump = false)
    {
        if (dump)
        {
            Debug.WriteLine(node.Kind());
            Debug.WriteLine(node.ToString());
        }

        return node is ClassDeclarationSyntax
               || node is PropertyDeclarationSyntax
               || node is EventDeclarationSyntax
               || node is VariableDeclaratorSyntax
               || node is EnumDeclarationSyntax
               || node is EnumMemberDeclarationSyntax
               || node is ConstructorDeclarationSyntax
               || node is DelegateDeclarationSyntax
               || node is MethodDeclarationSyntax
               || node is StructDeclarationSyntax
               || node is InterfaceDeclarationSyntax
               || node is TypeParameterSyntax
               || node is ParameterSyntax
               || node is AnonymousObjectMemberDeclaratorSyntax
               || node is ForEachStatementSyntax;
    }

    /// <summary>
    ///     Determines whether the given syntax node is a symbol declaration.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public static bool IsTypeDeclaration(this SyntaxNode node) =>
        node is ClassDeclarationSyntax
        || node is EnumDeclarationSyntax
        || node is DelegateDeclarationSyntax
        || node is StructDeclarationSyntax
        || node is InterfaceDeclarationSyntax
        || node is TypeParameterSyntax;

    class UnindentRewriter(int indentations) : CSharpSyntaxRewriter
    {
        readonly string _spaces = new(' ', indentations * 4);
        readonly string _tabs = new('\t', indentations);

        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia) => ReplaceTrivia(base.VisitTrivia(trivia));

        SyntaxTrivia ReplaceTrivia(SyntaxTrivia triv)
        {
            if (!triv.IsKind(SyntaxKind.WhitespaceTrivia))
                return triv;
            //return triv;
            var loc = triv.GetLocation();
            var ls = loc.GetLineSpan();
            if (ls.StartLinePosition.Character != 0)
                return triv;

            var triviaString = triv.ToFullString();
            if (triviaString.Equals(_tabs) || triviaString.Equals(_spaces))
                return triv.CopyAnnotationsTo(SyntaxFactory.Whitespace(""));

            if (triviaString.StartsWith(_tabs))
                return triv.CopyAnnotationsTo(SyntaxFactory.Whitespace(triviaString.Substring(_tabs.Length)));

            if (triviaString.StartsWith(_spaces))
                return triv.CopyAnnotationsTo(SyntaxFactory.Whitespace(triviaString.Substring(_spaces.Length)));

            return triv;
        }
    }
}