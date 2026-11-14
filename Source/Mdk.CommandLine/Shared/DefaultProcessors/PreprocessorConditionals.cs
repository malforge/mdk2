using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mdk.CommandLine.Shared.DefaultProcessors;

/// <summary>
///     Removes conditional compilation directives and the code they exclude, as the compiler would for the project's
///     preprocessor symbols.
/// </summary>
public class PreprocessorConditionals : IDocumentProcessor
{
    static readonly HashSet<SyntaxKind> RemovedKinds =
    [
        SyntaxKind.IfDirectiveTrivia,
        SyntaxKind.ElifDirectiveTrivia,
        SyntaxKind.ElseDirectiveTrivia,
        SyntaxKind.EndIfDirectiveTrivia,
        SyntaxKind.DefineDirectiveTrivia,
        SyntaxKind.UndefDirectiveTrivia,
        SyntaxKind.DisabledTextTrivia
    ];

    /// <inheritdoc />
    public async Task<Document> ProcessAsync(Document document, IPackContext context)
    {
        var existingRoot = await document.GetSyntaxRootAsync();
        if (existingRoot is null || !existingRoot.ContainsDirectives)
            return document;

        var sourceText = await document.GetTextAsync();
        var parseOptions = (document.Project.ParseOptions as CSharpParseOptions ?? CSharpParseOptions.Default)
            .WithPreprocessorSymbols(context.PreprocessorSymbols);
        var root = await CSharpSyntaxTree.ParseText(sourceText, parseOptions).GetRootAsync();

        var removedLines = new SortedSet<int>();
        foreach (var trivia in root.DescendantTrivia().Where(IsRemoved))
        {
            var span = trivia.Span;
            var first = sourceText.Lines.GetLineFromPosition(span.Start).LineNumber;
            var last = sourceText.Lines.GetLineFromPosition(span.End > span.Start ? span.End - 1 : span.Start).LineNumber;
            for (var line = first; line <= last; line++)
                removedLines.Add(line);
        }

        if (removedLines.Count == 0)
            return document;

        var changes = removedLines.Select(line => new TextChange(sourceText.Lines[line].SpanIncludingLineBreak, ""));
        return document.WithText(sourceText.WithChanges(changes));
    }

    // Roslyn lexes a directive inside an excluded section as an inactive directive of its own kind, not as disabled text.
    static bool IsRemoved(SyntaxTrivia trivia) =>
        RemovedKinds.Contains(trivia.Kind()) || trivia.GetStructure() is DirectiveTriviaSyntax { IsActive: false };
}
