using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

/// <summary>
///     The default implementation of the <see cref="IScriptCombiner" /> interface.
/// </summary>
/// <remarks>
///     This combiner will combine all the syntax trees into a single syntax tree, removing the namespace and unindenting
///     the code to compensate.
/// </remarks>
public class Combiner : IScriptCombiner
{
    readonly struct TreeWithWeight
    {
        static readonly string[] NewLine = { "\r\n", "\n" };
        
        public TreeWithWeight(SyntaxTree tree, int weight)
        {
            Tree = tree;
            //Weight = weight;
            if (MdkTrivia.TryGetMdkTrivia(Tree, out var trivia))
            {
                if (trivia.SortOrderSpecified)
                    Weight = trivia.SortOrder;
            }
        }

        public SyntaxTree Tree { get; }
        public int Weight { get; }
        
        public SyntaxNode GetRoot() => Tree.GetRoot();
    } 
    
    /// <inheritdoc />
    public async Task<Document> CombineAsync(Project project, IReadOnlyList<Document> documents, IPackContext context)
    {
        var trees = (await Task.WhenAll(documents.Select(async d => await d.GetSyntaxTreeAsync())))
            .Where(t => t is not null)
            .Select((t, i) => new TreeWithWeight(t!, i))
            .OrderBy(t => t.Weight)
            .ToList();

        var namespaceUsings = trees.SelectMany(t => t.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>() ?? [])
            .GroupBy(u => u.Name?.ToString()).Select(g => g.First()).ToArray();

        var typeDeclarations = trees.SelectMany(t => t.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>() ?? [])
            .Where(t => t is not NamespaceDeclarationSyntax && t.Parent is CompilationUnitSyntax or NamespaceDeclarationSyntax)
            .ToArray();

        var combinedSyntaxTree =
            (CSharpSyntaxTree)CSharpSyntaxTree.Create(
                SyntaxFactory.CompilationUnit()
                    .AddUsings(namespaceUsings)
                    .AddMembers(typeDeclarations)
            );

        var documentIds = documents.Select(d => d.Id).ToImmutableArray();
        var document = project.RemoveDocuments(documentIds)
            .AddDocument("script.cs", await combinedSyntaxTree.GetRootAsync());
        
        return await document.RemoveUnnecessaryUsingsAsync();
    }
}