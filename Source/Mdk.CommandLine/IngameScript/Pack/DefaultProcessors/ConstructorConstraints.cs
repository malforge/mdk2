using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

/// <summary>
///     Finds the types which are used as the type argument of a <c>new()</c> constrained type parameter
///     (e.g. <c>class Factory&lt;T&gt; where T : new()</c> used as <c>Factory&lt;Widget&gt;</c>).
/// </summary>
/// <remarks>
///     Such a type needs a public parameterless constructor even though nothing refers to that constructor
///     directly, so processors must neither remove it nor reduce its accessibility.
/// </remarks>
static class ConstructorConstraints
{
    /// <summary>
    ///     Collects every type used as the type argument of a <c>new()</c> constrained type parameter.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="semanticModel"></param>
    /// <returns></returns>
    public static HashSet<INamedTypeSymbol> Collect(SyntaxNode root, SemanticModel semanticModel)
    {
        var constrainedTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var genericName in root.DescendantNodes().OfType<GenericNameSyntax>())
        {
            switch (semanticModel.GetSymbolInfo(genericName).Symbol)
            {
                case INamedTypeSymbol namedTypeSymbol:
                    Add(constrainedTypes, namedTypeSymbol.TypeArguments, namedTypeSymbol.TypeParameters);
                    break;
                case IMethodSymbol methodSymbol:
                    Add(constrainedTypes, methodSymbol.TypeArguments, methodSymbol.TypeParameters);
                    break;
            }
        }
        return constrainedTypes;
    }

    static void Add(HashSet<INamedTypeSymbol> constrainedTypes, IReadOnlyList<ITypeSymbol> typeArguments, IReadOnlyList<ITypeParameterSymbol> typeParameters)
    {
        var count = Math.Min(typeArguments.Count, typeParameters.Count);
        for (var i = 0; i < count; i++)
        {
            if (!typeParameters[i].HasConstructorConstraint)
                continue;
            if (typeArguments[i] is INamedTypeSymbol argumentType)
                constrainedTypes.Add(argumentType);
        }
    }
}
