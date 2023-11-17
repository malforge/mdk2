using System;
using Microsoft.CodeAnalysis;

namespace Mdk2.ModAnalyzers
{
    /// <summary>
    ///     Roslyn does not provide a good way to compare a <see cref="Type" /> with an <see cref="ISymbol" />. These
    ///     extensions aim to provide "good enough" comparisons. In addition it adds a few other key types to be used
    ///     for the whitelist.
    /// </summary>
    static class TypeKeyExtensions
    {
        public static string GetWhitelistKey(this ISymbol symbol, TypeKeyQuantity quantity)
        {
            switch (symbol)
            {
                case INamespaceSymbol namespaceSymbol:
                    return namespaceSymbol.GetWhitelistKey(quantity);

                case ITypeSymbol typeSymbol:
                    return typeSymbol.GetWhitelistKey(quantity);

                // Account for generic methods, we must check their definitions, not specific implementations
                case IMethodSymbol methodSymbol when methodSymbol.IsGenericMethod && !methodSymbol.IsDefinition:
                {
                    methodSymbol = methodSymbol.OriginalDefinition;
                    if (methodSymbol.IsExtensionMethod && methodSymbol.ReducedFrom != null)
                        methodSymbol = methodSymbol.ReducedFrom;
                    return methodSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                           + ", "
                           + symbol.ContainingAssembly.Name;
                }
            }

            var memberSymbol = symbol is IEventSymbol || symbol is IFieldSymbol || symbol is IPropertySymbol || symbol is IMethodSymbol ? symbol : null;
            if (memberSymbol == null)
                throw new ArgumentException("Invalid symbol type: Expected namespace, type or type member", nameof(symbol));

            return memberSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                   + ", "
                   + symbol.ContainingAssembly.Name;
        }

        public static string GetWhitelistKey(this ITypeSymbol symbol, TypeKeyQuantity quantity)
        {
            symbol = ResolveRootType(symbol);

            switch (quantity)
            {
                case TypeKeyQuantity.ThisOnly:
                    return symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                           + ", "
                           + symbol.ContainingAssembly.Name;

                case TypeKeyQuantity.AllMembers:
                    return symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                           + "+*, "
                           + symbol.ContainingAssembly.Name;

                case TypeKeyQuantity.None:
                default:
                    throw new ArgumentOutOfRangeException(nameof(quantity), quantity, null);
            }
        }

        public static string GetWhitelistKey(this INamespaceSymbol symbol, TypeKeyQuantity quantity)
        {
            switch (quantity)
            {
                case TypeKeyQuantity.ThisOnly:
                    return symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                           + ", "
                           + symbol.ContainingAssembly.Name;

                case TypeKeyQuantity.AllMembers:
                    return symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                           + ".*, "
                           + symbol.ContainingAssembly.Name;

                default:
                    throw new ArgumentOutOfRangeException(nameof(quantity), quantity, null);
            }
        }

        static ITypeSymbol ResolveRootType(ITypeSymbol symbol)
        {
            if (symbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType && !namedTypeSymbol.IsDefinition)
            {
                symbol = namedTypeSymbol.OriginalDefinition;
                //if (symbol.SpecialType == SpecialType.System_Nullable_T)
                //    return namedTypeSymbol.TypeArguments[0];
                return symbol;
            }

            if (symbol is IPointerTypeSymbol pointerTypeSymbol)
                return pointerTypeSymbol.PointedAtType;

            return symbol;
        }
    }
}