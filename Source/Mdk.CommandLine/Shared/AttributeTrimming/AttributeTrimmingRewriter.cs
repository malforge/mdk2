using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mdk.CommandLine.Shared.AttributeTrimming;

public sealed class AttributeTrimmingRewriter(
    ImmutableHashSet<TextSpan> attributeApplications,
    ImmutableHashSet<TextSpan> attributeDeclarations,
    ImmutableHashSet<TextSpan> usingDirectives)
{
    public SyntaxNode Rewrite(SyntaxNode root)
    {
        var allDeclarationsToRemove = root.DescendantNodesAndSelf()
            .OfType<ClassDeclarationSyntax>()
            .Where(declaration => attributeDeclarations.Contains(declaration.Span))
            .ToArray();

        var declarationsToRemove = allDeclarationsToRemove
            .Where(declaration => !declaration.Ancestors()
                .OfType<ClassDeclarationSyntax>()
                .Any(ancestor => attributeDeclarations.Contains(ancestor.Span)))
            .Cast<SyntaxNode>()
            .ToArray();

        var declarationSpans = declarationsToRemove
            .Select(declaration => declaration.Span)
            .ToImmutableArray();

        var attributeLists = root.DescendantNodesAndSelf()
            .OfType<AttributeListSyntax>()
            .Where(attributeList => !IsWithinRemovedDeclaration(attributeList, declarationSpans))
            .Select(attributeList => new
            {
                Node = attributeList,
                KeptAttributes = attributeList.Attributes
                    .Where(attribute => !attributeApplications.Contains(attribute.Span))
                    .ToArray()
            })
            .Where(item => item.KeptAttributes.Length != item.Node.Attributes.Count)
            .ToArray();

        var attributeListsToRemove = attributeLists
            .Where(item => item.KeptAttributes.Length == 0)
            .Select(item => (SyntaxNode)item.Node)
            .ToArray();

        var attributeListsToRewrite = attributeLists
            .Where(item => item.KeptAttributes.Length > 0)
            .ToDictionary(item => item.Node, item => item.KeptAttributes);

        var usingsToRemove = root.DescendantNodesAndSelf()
            .OfType<UsingDirectiveSyntax>()
            .Where(usingDirective => usingDirectives.Contains(usingDirective.Span))
            .Where(usingDirective => !IsWithinRemovedDeclaration(usingDirective, declarationSpans))
            .Cast<SyntaxNode>()
            .ToArray();

        var nodesToRemove = declarationsToRemove
            .Concat(attributeListsToRemove)
            .Concat(usingsToRemove)
            .ToArray();

        var nodesToTrack = nodesToRemove
            .Concat(attributeListsToRewrite.Keys)
            .Distinct()
            .ToArray();

        if (nodesToTrack.Length == 0)
            return root;

        var rewrittenRoot = root.TrackNodes(nodesToTrack);

        foreach (var (attributeList, keptAttributes) in attributeListsToRewrite)
        {
            var currentAttributeList = rewrittenRoot.GetCurrentNode(attributeList);
            if (currentAttributeList == null)
                continue;

            rewrittenRoot = rewrittenRoot.ReplaceNode(
                currentAttributeList,
                currentAttributeList.WithAttributes(SyntaxFactory.SeparatedList(keptAttributes)));
        }

        var currentNodesToRemove = nodesToRemove
            .Select(node => rewrittenRoot.GetCurrentNode(node))
            .Where(node => node != null)
            .Cast<SyntaxNode>()
            .ToArray();

        if (currentNodesToRemove.Length == 0)
            return rewrittenRoot;

        return rewrittenRoot.RemoveNodes(
                   currentNodesToRemove,
                   SyntaxRemoveOptions.KeepExteriorTrivia
                   | SyntaxRemoveOptions.KeepUnbalancedDirectives
                   | SyntaxRemoveOptions.AddElasticMarker)
               ?? rewrittenRoot;
    }

    static bool IsWithinRemovedDeclaration(SyntaxNode node, ImmutableArray<TextSpan> declarationSpans)
    {
        return declarationSpans.Any(span => span.Start <= node.SpanStart && span.End >= node.Span.End);
    }
}
