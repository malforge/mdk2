using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk2.ModAnalyzers
{
    static class AttributeContext
    {
        public static bool IsAllowed(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return IsInsideSourceDefinedAttributeApplication(node, semanticModel, cancellationToken)
                   || IsInsideSourceDefinedAttributeDeclaration(node, semanticModel, cancellationToken);
        }

        public static bool IsSourceDefinedAttributeTypeReference(ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var attributeType = ResolveReferencedAttributeType(symbol);
            return attributeType != null && IsSourceDefinedAttributeType(attributeType, semanticModel.Compilation, cancellationToken);
        }

        static bool IsInsideSourceDefinedAttributeApplication(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var attribute = node.AncestorsAndSelf().OfType<AttributeSyntax>().FirstOrDefault();
            if (attribute == null)
                return false;

            var attributeType = ResolveAttributeType(semanticModel, attribute, cancellationToken);
            if (attributeType == null)
                return false;

            return IsSourceDefinedAttributeType(attributeType, semanticModel.Compilation, cancellationToken);
        }

        static bool IsInsideSourceDefinedAttributeDeclaration(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            foreach (var classDeclaration in node.AncestorsAndSelf().OfType<ClassDeclarationSyntax>())
            {
                var symbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) as INamedTypeSymbol;
                if (symbol != null && IsSourceDefinedAttributeType(symbol, semanticModel.Compilation, cancellationToken))
                    return true;
            }

            return false;
        }

        static bool IsSourceDefinedAttributeType(INamedTypeSymbol symbol, Compilation compilation, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!symbol.Locations.Any(location => location.IsInSource))
                return false;

            var systemAttribute = compilation.GetTypeByMetadataName("System.Attribute");
            if (systemAttribute == null)
                return false;

            for (var current = symbol; current != null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, systemAttribute))
                    return true;
            }

            return false;
        }

        static INamedTypeSymbol ResolveAttributeType(SemanticModel model, AttributeSyntax attribute, CancellationToken cancellationToken)
        {
            var info = model.GetSymbolInfo(attribute, cancellationToken);
            if (info.Symbol is IMethodSymbol methodSymbol)
                return methodSymbol.ContainingType;

            if (info.Symbol is INamedTypeSymbol namedTypeSymbol)
                return namedTypeSymbol.TypeKind == TypeKind.Error ? null : namedTypeSymbol;

            foreach (var candidate in info.CandidateSymbols)
            {
                if (candidate is IMethodSymbol candidateMethod)
                    return candidateMethod.ContainingType;
                if (candidate is INamedTypeSymbol candidateType && candidateType.TypeKind != TypeKind.Error)
                    return candidateType;
            }

            return null;
        }

        static INamedTypeSymbol ResolveReferencedAttributeType(ISymbol symbol)
        {
            if (symbol is IAliasSymbol aliasSymbol)
                symbol = aliasSymbol.Target;

            if (symbol is INamedTypeSymbol namedTypeSymbol)
                return namedTypeSymbol;

            if (symbol is IMethodSymbol methodSymbol && methodSymbol.MethodKind == MethodKind.Constructor)
                return methodSymbol.ContainingType;

            return symbol.ContainingType;
        }
    }
}
