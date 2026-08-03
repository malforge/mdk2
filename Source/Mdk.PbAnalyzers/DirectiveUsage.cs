using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk2.PbAnalyzers
{
    /// <summary>
    ///     Works out whether a using directive is actually doing anything for the file it sits in.
    /// </summary>
    /// <remarks>
    ///     The compiler answers this with CS8019, but <see cref="SemanticModel.GetDiagnostics" /> returns nothing at all
    ///     when analyzers run inside csc, so a rule that asked for it would quietly do nothing in a real build while
    ///     appearing to work everywhere else. Hence the direct approach.
    /// </remarks>
    static class DirectiveUsage
    {
        /// <summary>
        ///     Whether removing this directive would change how any name in the file binds.
        /// </summary>
        public static bool IsUsed(UsingDirectiveSyntax directive, SemanticModel model, SyntaxNode root, CancellationToken cancellationToken)
        {
            if (directive.Alias != null)
                return IsAliasUsed(directive.Alias.Name.Identifier.ValueText, root);

            var target = model.GetSymbolInfo(directive.Name, cancellationToken).Symbol;
            if (target == null)
                return false;

            if (directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
                return IsStaticImportUsed(target, model, root, cancellationToken);

            var namespaceSymbol = target as INamespaceSymbol;
            if (namespaceSymbol == null)
                return false;

            return IsNamespaceUsed(namespaceSymbol, model, root, cancellationToken);
        }

        static bool IsAliasUsed(string alias, SyntaxNode root)
        {
            foreach (var name in root.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                if (IsInsideUsingDirective(name))
                    continue;
                if (name.Identifier.ValueText == alias)
                    return true;
            }

            return false;
        }

        static bool IsStaticImportUsed(ISymbol target, SemanticModel model, SyntaxNode root, CancellationToken cancellationToken)
        {
            var targetType = target as INamedTypeSymbol;
            if (targetType == null)
                return false;

            foreach (var name in root.DescendantNodes().OfType<SimpleNameSyntax>())
            {
                if (IsInsideUsingDirective(name) || IsQualifiedAccess(name))
                    continue;

                var symbol = model.GetSymbolInfo(name, cancellationToken).Symbol;
                if (symbol?.ContainingType == null)
                    continue;

                if (!SymbolEqualityComparer.Default.Equals(symbol.ContainingType, targetType))
                    continue;

                // Code sitting inside the type - or inheriting from it - reaches these members through the nesting or
                // the base type, so the import is not what makes this name resolve. Scripts do this by accident:
                // "using static IngameScript.Program" on a class nested in Program was never needed.
                if (IsWithinOrInherits(model, name, targetType, cancellationToken))
                    continue;

                return true;
            }

            return false;
        }

        static bool IsWithinOrInherits(SemanticModel model, SyntaxNode node, INamedTypeSymbol targetType, CancellationToken cancellationToken)
        {
            var enclosing = model.GetEnclosingSymbol(node.SpanStart, cancellationToken);
            for (var symbol = enclosing; symbol != null; symbol = symbol.ContainingSymbol)
            {
                var type = symbol as INamedTypeSymbol;
                if (type == null)
                    continue;

                for (var candidate = type; candidate != null; candidate = candidate.BaseType)
                {
                    if (SymbolEqualityComparer.Default.Equals(candidate, targetType))
                        return true;
                }
            }

            return false;
        }

        static bool IsNamespaceUsed(INamespaceSymbol namespaceSymbol, SemanticModel model, SyntaxNode root, CancellationToken cancellationToken)
        {
            foreach (var name in root.DescendantNodes().OfType<SimpleNameSyntax>())
            {
                if (IsInsideUsingDirective(name))
                    continue;

                var symbol = model.GetSymbolInfo(name, cancellationToken).Symbol;
                if (symbol == null)
                    continue;

                // An extension method invoked on a value is the one case where a name that looks qualified still needs
                // the namespace imported.
                var method = symbol as IMethodSymbol;
                if (method != null && method.MethodKind == MethodKind.ReducedExtension)
                {
                    if (Matches(method.ContainingType?.ContainingNamespace, namespaceSymbol))
                        return true;
                    continue;
                }

                // Anything written out with a qualifier resolves without the import.
                if (IsQualifiedAccess(name))
                    continue;

                // Only a type name written unqualified can require the import. Members, parameters and the like are
                // always reached through something whose type is already established, so they say nothing about whether
                // the directive is pulling its weight - counting them treated `regex?.IsMatch(x)` as needing an import
                // that the fully qualified code around it plainly did not.
                var containing = TypeNamespaceOf(symbol);
                if (Matches(containing, namespaceSymbol))
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     The namespace an import would have to supply for this symbol to be nameable, or <c>null</c> if the symbol
        ///     is not a type reference at all.
        /// </summary>
        static INamespaceSymbol TypeNamespaceOf(ISymbol symbol)
        {
            var type = symbol as ITypeSymbol;
            if (type != null)
                return type.ContainingNamespace;

            // An attribute is written as a type name but binds to its constructor.
            var method = symbol as IMethodSymbol;
            if (method != null && method.MethodKind == MethodKind.Constructor)
                return method.ContainingType?.ContainingNamespace;

            return null;
        }

        static bool Matches(INamespaceSymbol candidate, INamespaceSymbol namespaceSymbol)
            => candidate != null && !candidate.IsGlobalNamespace && candidate.ToDisplayString() == namespaceSymbol.ToDisplayString();

        static bool IsInsideUsingDirective(SyntaxNode node)
        {
            for (var current = node.Parent; current != null; current = current.Parent)
            {
                if (current is UsingDirectiveSyntax)
                    return true;
                if (current is MemberDeclarationSyntax)
                    return false;
            }

            return false;
        }

        /// <summary>
        ///     Whether the name is the trailing part of something already qualified, and so resolves without any import.
        /// </summary>
        static bool IsQualifiedAccess(SyntaxNode node)
        {
            var qualified = node.Parent as QualifiedNameSyntax;
            if (qualified != null)
                return qualified.Right == node;

            var memberAccess = node.Parent as MemberAccessExpressionSyntax;
            if (memberAccess != null)
                return memberAccess.Name == node;

            // The null conditional form, x?.Y, puts the name in a member binding rather than a member access.
            var memberBinding = node.Parent as MemberBindingExpressionSyntax;
            if (memberBinding != null)
                return memberBinding.Name == node;

            var aliasQualified = node.Parent as AliasQualifiedNameSyntax;
            return aliasQualified != null && aliasQualified.Name == node;
        }
    }
}
