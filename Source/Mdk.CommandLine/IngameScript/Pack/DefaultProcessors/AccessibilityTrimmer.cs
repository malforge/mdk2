using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

/// <summary>
///     Removes the <c>public</c> modifier from every declaration in the script which does not need it.
/// </summary>
/// <remarks>
///     <para>
///         The packed script becomes the body of the game's <c>Program</c> class, so a type declared in it is either a
///         nested type of <c>Program</c> or a sibling of <c>Program</c>. A nested type without an accessibility modifier
///         is private to its container, which every other declaration inside that container can still reach, and a
///         top level type without one is internal, which the whole script can reach. A member without one is private to
///         the type that declares it, and a sibling type can no longer reach it, so <c>public</c> is only removed from a
///         member when every reference to it is inside the declaring type.
///     </para>
///     <para>
///         Declarations which the language or the game requires to be public are left alone: implicit interface
///         implementations, virtual, abstract and overriding members, operators, the parameterless constructor of a type
///         used as a <c>new()</c> constrained type argument, and the <c>Program</c> entry points (the constructor,
///         <c>Main</c> and <c>Save</c>) which <see cref="SymbolProtectionAnnotator" /> marks as protected symbols.
///         A type is also kept public when it is exposed by a signature which stays public, because a type may not be
///         less accessible than a declaration that exposes it.
///     </para>
/// </remarks>
[RunAfter<PartialMerger>]
[RunAfter<RegionAnnotator>]
[RunAfter<TypeSorter>]
[RunAfter<SymbolProtectionAnnotator>]
[RunBefore<CodeSmallifier>]
public class AccessibilityTrimmer : IDocumentProcessor
{
    /// <inheritdoc />
    public async Task<Document> ProcessAsync(Document document, IPackContext context)
    {
        if (context.Parameters.PackVerb.MinifierLevel < MinifierLevel.Trim)
        {
            context.Console.Trace("Skipping accessibility trimming because the minifier level < Trim.");
            return document;
        }

        var root = await document.GetSyntaxRootAsync();
        if (root == null)
            return document;
        var semanticModel = await document.GetSemanticModelAsync();
        if (semanticModel == null)
            return document;

        var declarations = await new Analysis(document, root, semanticModel).FindTrimmableDeclarationsAsync();
        if (declarations.Count == 0)
        {
            context.Console.Trace("No public modifiers could be removed.");
            return document;
        }

        context.Console.Trace($"Removing the public modifier from {declarations.Count} declaration(s).");
        var newRoot = root.ReplaceNodes(declarations, (_, rewritten) => Trim(rewritten));
        return document.WithSyntaxRoot(newRoot);
    }

    /// <summary>
    ///     Removes the <c>public</c> modifier from the declaration, and with it any accessibility modifier on its
    ///     accessors: an accessor may only be more restrictive than the property or event it belongs to (CS0273), and
    ///     once the declaration itself is private there is nothing left to restrict.
    /// </summary>
    /// <param name="declaration"></param>
    /// <returns></returns>
    static MemberDeclarationSyntax Trim(MemberDeclarationSyntax declaration)
    {
        if (declaration is BasePropertyDeclarationSyntax { AccessorList: not null } property)
        {
            var accessors = property.AccessorList!.Accessors;
            declaration = property.WithAccessorList(property.AccessorList.WithAccessors(
                SyntaxFactory.List(accessors.Select(accessor => accessor.WithoutAccessibilityModifiers()))));
        }

        return declaration.WithoutModifier(SyntaxKind.PublicKeyword);
    }

    /// <summary>
    ///     The analysis of a single document: which public declarations can lose the modifier.
    /// </summary>
    class Analysis
    {
        readonly HashSet<INamedTypeSymbol> _constructorConstrainedTypes;
        readonly Document _document;
        readonly Dictionary<ISymbol, List<TextSpan>> _implicitUses;
        readonly SyntaxNode _root;
        readonly IImmutableSet<Document> _scope;
        readonly SemanticModel _semanticModel;
        readonly HashSet<MemberDeclarationSyntax> _trimmableMembers = new();
        readonly List<TypeCandidate> _typeCandidates = new();
        readonly Dictionary<MemberDeclarationSyntax, TypeCandidate> _typeCandidatesByDeclaration = new();

        public Analysis(Document document, SyntaxNode root, SemanticModel semanticModel)
        {
            _document = document;
            _root = root;
            _semanticModel = semanticModel;
            _scope = ImmutableHashSet.Create(document);
            _implicitUses = CollectImplicitUses(root, semanticModel);
            _constructorConstrainedTypes = ConstructorConstraints.Collect(root, semanticModel);
        }

        /// <summary>
        ///     Determines which declarations can lose their <c>public</c> modifier.
        /// </summary>
        /// <returns></returns>
        public async Task<List<MemberDeclarationSyntax>> FindTrimmableDeclarationsAsync()
        {
            foreach (var declaration in _root.DescendantNodes().OfType<MemberDeclarationSyntax>())
            {
                if (!declaration.Modifiers.Any(SyntaxKind.PublicKeyword))
                    continue;
                if (IsPreserved(declaration))
                    continue;

                if (declaration is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
                {
                    var candidate = await CreateTypeCandidateAsync(declaration);
                    if (candidate == null)
                        continue;
                    _typeCandidates.Add(candidate);
                    _typeCandidatesByDeclaration.Add(declaration, candidate);
                    continue;
                }

                if (await CanTrimMemberAsync(declaration))
                    _trimmableMembers.Add(declaration);
            }

            KeepExposedTypesPublic();

            var result = new List<MemberDeclarationSyntax>(_trimmableMembers);
            result.AddRange(_typeCandidates.Where(candidate => candidate.Trim).Select(candidate => candidate.Declaration));
            return result;
        }

        /// <summary>
        ///     Determines whether the accessibility of a member can be reduced to private. It can when nothing outside the
        ///     declaring type refers to it, and neither the language nor the game requires it to be public.
        /// </summary>
        /// <param name="declaration"></param>
        /// <returns></returns>
        async Task<bool> CanTrimMemberAsync(MemberDeclarationSyntax declaration)
        {
            var symbols = GetDeclaredSymbols(declaration);
            if (symbols == null)
                return false;

            foreach (var symbol in symbols)
            {
                var containingType = symbol.ContainingType;
                if (containingType == null)
                    return false;
                // Interface members cannot carry accessibility modifiers at all, so leave them as written.
                if (containingType.TypeKind == TypeKind.Interface)
                    return false;
                // A private member cannot be virtual or abstract, an override has to keep the accessibility of what it
                // overrides, and an implicit interface implementation has to be public.
                if (symbol.IsAbstract || symbol.IsVirtual || symbol.IsOverride || symbol.IsExtern)
                    return false;
                if (symbol.IsInterfaceImplementation())
                    return false;
                if (symbol is IMethodSymbol method)
                {
                    // Operators and conversions must be public, and accessors are rewritten with their property.
                    if (method.MethodKind != MethodKind.Ordinary && method.MethodKind != MethodKind.Constructor)
                        return false;
                    // A new() constrained type argument needs its public parameterless constructor.
                    if (method.MethodKind == MethodKind.Constructor && method.Parameters.Length == 0 && _constructorConstrainedTypes.Contains(containingType))
                        return false;
                }
                if (!await AllReferencesAreWithinAsync(symbol, containingType))
                    return false;
            }

            return true;
        }

        /// <summary>
        ///     Determines whether the type declaration can lose its <c>public</c> modifier, and records the places where
        ///     reducing its accessibility would be visible in a signature.
        /// </summary>
        /// <param name="declaration"></param>
        /// <returns>A candidate, or <c>null</c> when the type has to stay public.</returns>
        async Task<TypeCandidate?> CreateTypeCandidateAsync(MemberDeclarationSyntax declaration)
        {
            if (_semanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol)
                return null;
            // The Program class itself is the game's entry point class; its declaration is never rewritten.
            if (symbol.ContainingType == null && symbol.Name == "Program")
                return null;

            var containingType = symbol.ContainingType;
            var containerSpans = containingType == null ? ImmutableArray<TextSpan>.Empty : DeclarationSpans(containingType);
            if (containingType != null && containerSpans.IsEmpty)
                return null;

            var locations = await FindReferenceSpansAsync(symbol);
            if (locations == null)
                return null;

            var candidate = new TypeCandidate(declaration, containingType != null, containerSpans);
            foreach (var location in locations)
            {
                // A nested type becomes private to its container, so nothing outside the container may refer to it.
                if (candidate.IsNested && !containerSpans.Any(span => span.Contains(location)))
                    return null;
                var referenceNode = _root.FindNode(location, getInnermostNodeForTie: true);
                if (IsInSignaturePosition(referenceNode))
                    candidate.ExposureSites.Add(referenceNode);
            }

            return candidate;
        }

        /// <summary>
        ///     Keeps a candidate type public when a declaration which stays public exposes it, since a type may never be
        ///     less accessible than a declaration that exposes it. Repeats until nothing changes, because keeping one type
        ///     public can expose another.
        /// </summary>
        void KeepExposedTypesPublic()
        {
            bool changed;
            do
            {
                changed = false;
                foreach (var candidate in _typeCandidates)
                {
                    if (!candidate.Trim)
                        continue;
                    if (candidate.ExposureSites.All(site => IsConfined(site, candidate)))
                        continue;
                    candidate.Trim = false;
                    changed = true;
                }
            } while (changed);
        }

        /// <summary>
        ///     Determines whether everything between the reference and the container of the candidate type is narrow
        ///     enough that the reduced accessibility of that type is still sufficient.
        /// </summary>
        /// <param name="referenceNode"></param>
        /// <param name="candidate"></param>
        /// <returns></returns>
        bool IsConfined(SyntaxNode referenceNode, TypeCandidate candidate)
        {
            for (var node = referenceNode; node != null; node = node.Parent)
            {
                if (node is not MemberDeclarationSyntax declaration)
                    continue;
                if (candidate.ContainerSpans.Any(span => span == declaration.Span))
                    return false;
                var accessibility = EffectiveAccessibility(declaration);
                // A nested candidate becomes private to its container: only a private link confines the reference to
                // the same container. A top level candidate becomes internal, so an internal link is enough as well.
                if (accessibility == Accessibility.Private)
                    return true;
                if (!candidate.IsNested && accessibility is Accessibility.Internal or Accessibility.ProtectedAndInternal)
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     The accessibility a declaration will have once this processor is done with it.
        /// </summary>
        /// <param name="declaration"></param>
        /// <returns></returns>
        Accessibility EffectiveAccessibility(MemberDeclarationSyntax declaration)
        {
            if (_trimmableMembers.Contains(declaration))
                return Accessibility.Private;
            if (_typeCandidatesByDeclaration.TryGetValue(declaration, out var candidate) && candidate.Trim)
                return candidate.IsNested ? Accessibility.Private : Accessibility.Internal;
            var symbols = GetDeclaredSymbols(declaration);
            return symbols is { Count: > 0 } ? symbols[0].DeclaredAccessibility : Accessibility.Public;
        }

        /// <summary>
        ///     Determines whether every reference to the symbol is inside the given type.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="containingType"></param>
        /// <returns></returns>
        async Task<bool> AllReferencesAreWithinAsync(ISymbol symbol, INamedTypeSymbol containingType)
        {
            var spans = DeclarationSpans(containingType);
            if (spans.IsEmpty)
                return false;
            var locations = await FindReferenceSpansAsync(symbol);
            return locations != null && locations.All(location => spans.Any(span => span.Contains(location)));
        }

        /// <summary>
        ///     Finds every place in the document which refers to the symbol, including the implicit references which a
        ///     reference search does not report.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns>The spans of the references, or <c>null</c> if the symbol is referred to from another document.</returns>
        async Task<IReadOnlyList<TextSpan>?> FindReferenceSpansAsync(ISymbol symbol)
        {
            var spans = new List<TextSpan>();
            var references = await SymbolFinder.FindReferencesAsync(symbol, _document.Project.Solution, _scope, CancellationToken.None);
            foreach (var reference in references)
            {
                foreach (var location in reference.Locations)
                {
                    // A reference from outside the packed document cannot be confined to anything inside it.
                    if (location.Document.Id != _document.Id)
                        return null;
                    spans.Add(location.Location.SourceSpan);
                }
            }
            if (_implicitUses.TryGetValue(symbol, out var implicitSpans))
                spans.AddRange(implicitSpans);
            return spans;
        }

        /// <summary>
        ///     The spans of the declarations of a type within the document being processed.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        ImmutableArray<TextSpan> DeclarationSpans(INamedTypeSymbol symbol) =>
            [..symbol.DeclaringSyntaxReferences.Where(reference => reference.SyntaxTree == _root.SyntaxTree).Select(reference => reference.Span)];

        /// <summary>
        ///     The symbols declared by a declaration, or <c>null</c> when any of them cannot be resolved.
        /// </summary>
        /// <param name="declaration"></param>
        /// <returns></returns>
        IReadOnlyList<ISymbol>? GetDeclaredSymbols(MemberDeclarationSyntax declaration)
        {
            if (declaration is BaseFieldDeclarationSyntax field)
            {
                var symbols = new List<ISymbol>(field.Declaration.Variables.Count);
                foreach (var variable in field.Declaration.Variables)
                {
                    var symbol = _semanticModel.GetDeclaredSymbol(variable);
                    if (symbol == null)
                        return null;
                    symbols.Add(symbol);
                }
                return symbols;
            }

            var declaredSymbol = _semanticModel.GetDeclaredSymbol(declaration);
            return declaredSymbol == null ? null : [declaredSymbol];
        }

        /// <summary>
        ///     Collects the references which a reference search does not report because the source does not name the
        ///     symbol: the enumerator pattern of a foreach statement and the Add method of a collection initializer.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="semanticModel"></param>
        /// <returns></returns>
        static Dictionary<ISymbol, List<TextSpan>> CollectImplicitUses(SyntaxNode root, SemanticModel semanticModel)
        {
            var uses = new Dictionary<ISymbol, List<TextSpan>>(SymbolEqualityComparer.Default);

            void add(ISymbol? symbol, TextSpan span)
            {
                if (symbol == null)
                    return;
                var definition = symbol.OriginalDefinition;
                if (!uses.TryGetValue(definition, out var spans))
                    uses[definition] = spans = [];
                spans.Add(span);
            }

            foreach (var node in root.DescendantNodes())
            {
                switch (node)
                {
                    case CommonForEachStatementSyntax forEachStatement:
                        var info = semanticModel.GetForEachStatementInfo(forEachStatement);
                        add(info.GetEnumeratorMethod, node.Span);
                        add(info.MoveNextMethod, node.Span);
                        add(info.CurrentProperty, node.Span);
                        add(info.DisposeMethod, node.Span);
                        break;

                    case InitializerExpressionSyntax initializer when initializer.IsKind(SyntaxKind.CollectionInitializerExpression):
                        foreach (var element in initializer.Expressions)
                            add(semanticModel.GetCollectionInitializerSymbolInfo(element).Symbol, element.Span);
                        break;
                }
            }

            return uses;
        }

        /// <summary>
        ///     Determines whether a reference is part of a declaration's signature, where the accessibility of the
        ///     referenced type is constrained by the accessibility of the declaration. References inside bodies,
        ///     initializers and attributes are not.
        /// </summary>
        /// <param name="referenceNode"></param>
        /// <returns></returns>
        static bool IsInSignaturePosition(SyntaxNode referenceNode)
        {
            for (var node = referenceNode; node != null; node = node.Parent)
            {
                switch (node)
                {
                    case BlockSyntax:
                    case ArrowExpressionClauseSyntax:
                    case AccessorListSyntax:
                    case EqualsValueClauseSyntax:
                    case AttributeListSyntax:
                        return false;

                    case BaseTypeDeclarationSyntax:
                    case DelegateDeclarationSyntax:
                    case BaseMethodDeclarationSyntax:
                    case BasePropertyDeclarationSyntax:
                    case BaseFieldDeclarationSyntax:
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Determines whether the declaration is marked as a protected symbol, either because it is a Program entry
        ///     point or because it sits in a preserved region.
        /// </summary>
        /// <param name="declaration"></param>
        /// <returns></returns>
        static bool IsPreserved(MemberDeclarationSyntax declaration)
        {
            if (declaration.ShouldBePreserved())
                return true;
            return declaration switch
            {
                BaseTypeDeclarationSyntax type => type.Identifier.ShouldBePreserved(),
                DelegateDeclarationSyntax @delegate => @delegate.Identifier.ShouldBePreserved(),
                MethodDeclarationSyntax method => method.Identifier.ShouldBePreserved(),
                ConstructorDeclarationSyntax constructor => constructor.Identifier.ShouldBePreserved(),
                PropertyDeclarationSyntax property => property.Identifier.ShouldBePreserved(),
                EventDeclarationSyntax @event => @event.Identifier.ShouldBePreserved(),
                BaseFieldDeclarationSyntax field => field.Declaration.Variables.Any(variable => variable.Identifier.ShouldBePreserved()),
                _ => false
            };
        }

        class TypeCandidate(MemberDeclarationSyntax declaration, bool isNested, ImmutableArray<TextSpan> containerSpans)
        {
            public ImmutableArray<TextSpan> ContainerSpans { get; } = containerSpans;
            public MemberDeclarationSyntax Declaration { get; } = declaration;
            public List<SyntaxNode> ExposureSites { get; } = [];
            public bool IsNested { get; } = isNested;
            public bool Trim { get; set; } = true;
        }
    }
}
