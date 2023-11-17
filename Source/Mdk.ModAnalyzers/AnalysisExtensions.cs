using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk2.ModAnalyzers
{
    /// <summary>
    ///     Contains various utilities used by the scripting engine.
    /// </summary>
    static class AnalysisExtensions
    {
        public static ISymbol GetOverriddenSymbol(this ISymbol symbol)
        {
            if (!symbol.IsOverride)
                return null;
            switch (symbol)
            {
                case ITypeSymbol typeSymbol:
                    return typeSymbol.BaseType;
                case IEventSymbol eventSymbol:
                    return eventSymbol.OverriddenEvent;
                case IPropertySymbol propertySymbol:
                    return propertySymbol.OverriddenProperty;
                case IMethodSymbol methodSymbol:
                    return methodSymbol.OverriddenMethod;
                default:
                    return null;
            }
        }

        public static bool IsMemberSymbol(this ISymbol symbol) => symbol is IEventSymbol || symbol is IFieldSymbol || symbol is IPropertySymbol || symbol is IMethodSymbol;

        public static BaseMethodDeclarationSyntax WithBody(this BaseMethodDeclarationSyntax item, BlockSyntax body)
        {
            switch (item)
            {
                case ConstructorDeclarationSyntax cons:
                    return cons.WithBody(body);
                case ConversionOperatorDeclarationSyntax conv:
                    return conv.WithBody(body);
                case DestructorDeclarationSyntax dest:
                    return dest.WithBody(body);
                case MethodDeclarationSyntax meth:
                    return meth.WithBody(body);
                case OperatorDeclarationSyntax oper:
                    return oper.WithBody(body);
                default:
                    throw new ArgumentException("Unknown " + typeof(BaseMethodDeclarationSyntax).FullName, nameof(item));
            }

        }

        public static AnonymousFunctionExpressionSyntax WithBody(this AnonymousFunctionExpressionSyntax item, CSharpSyntaxNode body)
        {
            switch (item)
            {
                case AnonymousMethodExpressionSyntax anon:
                    return anon.WithBody(body);
                case ParenthesizedLambdaExpressionSyntax plam:
                    return plam.WithBody(body);
                case SimpleLambdaExpressionSyntax slam:
                    return slam.WithBody(body);
                default:
                    throw new ArgumentException("Unknown " + typeof(AnonymousFunctionExpressionSyntax).FullName, nameof(item));
            }

        }

        public static bool IsInSource(this ISymbol symbol)
        {
            for (var i = 0; i < symbol.Locations.Length; i++)
            {
                if (!symbol.Locations[i].IsInSource)
                    return false;
            }

            return true;
        }
    }
}