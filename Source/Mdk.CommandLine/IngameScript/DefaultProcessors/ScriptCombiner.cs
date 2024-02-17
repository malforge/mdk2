using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Api;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mdk.CommandLine.IngameScript.DefaultProcessors;

/// <summary>
///     The default implementation of the <see cref="IScriptCombiner" /> interface.
/// </summary>
/// <remarks>
///     This combiner will combine all the syntax trees into a single syntax tree, removing the namespace and unindenting
///     the code to compensate.
/// </remarks>
public class ScriptCombiner : IScriptCombiner
{
    public virtual async Task<CSharpSyntaxTree> CombineAsync(IReadOnlyList<CSharpSyntaxTree> syntaxTree, ScriptProjectMetadata metadata)
    {
        var namespaceUsings = syntaxTree.SelectMany(t => t.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>())
            .GroupBy(u => u.Name?.ToString()).Select(g => g.First()).ToArray();

        var typeDeclarations = syntaxTree.SelectMany(t => t.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>())
            .Where(t => t is not NamespaceDeclarationSyntax && t.Parent is CompilationUnitSyntax or NamespaceDeclarationSyntax)
            .ToArray();

        // typeDeclarations = await Task.WhenAll(typeDeclarations.Select(t => UnindentAsync(t, 4)));

        var combinedSyntaxTree =
            (CSharpSyntaxTree)CSharpSyntaxTree.Create(
                SyntaxFactory.CompilationUnit()
                    .AddUsings(namespaceUsings)
                    .AddMembers(typeDeclarations)
            );

        return combinedSyntaxTree;
    }

    // private static async Task<MemberDeclarationSyntax> UnindentAsync(MemberDeclarationSyntax typeDeclaration, int indentation)
    // {
    //     var text = await typeDeclaration.SyntaxTree.GetTextAsync();
    //     var buffer = new StringBuilder((int)(text.Length * 0.5));
    //     var span = typeDeclaration.Span;
    //     var startOfLine = text.Lines.GetLineFromPosition(span.Start).Start;
    //     var endOfLine = text.Lines.GetLineFromPosition(span.End).End;
    //     var alteredSpan = new TextSpan(startOfLine, endOfLine - startOfLine);
    //     buffer.AppendLine(text.ToString(alteredSpan));
    //     ConvertTabsToSpaces(buffer, indentation);
    //     Unindent(buffer, indentation);
    //
    //     return (MemberDeclarationSyntax)(await CSharpSyntaxTree.ParseText(buffer.ToString()).GetRootAsync()).ChildNodes().First();
    // }
    //
    // private static void Unindent(StringBuilder buffer, int indentation)
    // {
    //     var indentString = new string(' ', indentation);
    //     if (!IsIndented(buffer, indentString))
    //         return;
    //
    //     var startOfLine = 0;
    //     var endOfLine = buffer.FindNewLine(0);
    //     while (endOfLine != -1)
    //     {
    //         if (IsLineWhitespace(buffer, startOfLine, endOfLine))
    //         {
    //             startOfLine = buffer.SkipNewLine(endOfLine);
    //             endOfLine = buffer.FindNewLine(startOfLine);
    //             continue;
    //         }
    //
    //         if (buffer.StartsWith(startOfLine, indentString))
    //         {
    //             buffer.Remove(startOfLine, indentation);
    //             startOfLine = buffer.SkipNewLine(endOfLine - indentation);
    //             endOfLine = buffer.FindNewLine(startOfLine);
    //             continue;
    //         }
    //
    //         startOfLine = buffer.FindNewLine(endOfLine);
    //         endOfLine = buffer.FindNewLine(startOfLine);
    //     }
    // }
    //
    // private static bool IsIndented(StringBuilder buffer, string indentString)
    // {
    //     var startOfLine = 0;
    //     var endOfLine = buffer.FindNewLine(0);
    //     while (endOfLine != -1)
    //     {
    //         if (IsLineWhitespace(buffer, startOfLine, endOfLine))
    //         {
    //             startOfLine = buffer.SkipNewLine(endOfLine);
    //             endOfLine = buffer.FindNewLine(startOfLine);
    //             continue;
    //         }
    //
    //         if (!buffer.StartsWith(startOfLine, indentString))
    //             return false;
    //         startOfLine = buffer.SkipNewLine(endOfLine);
    //         endOfLine = buffer.FindNewLine(startOfLine);
    //     }
    //
    //     return true;
    // }
    //
    // private static bool IsLineWhitespace(StringBuilder buffer, int startOfLine, int endOfLine)
    // {
    //     for (var i = startOfLine; i < endOfLine; i++)
    //         if (!char.IsWhiteSpace(buffer[i]))
    //             return false;
    //     return true;
    // }
    //
    // private static void ConvertTabsToSpaces(StringBuilder buffer, int indentation)
    // {
    //     var chIndex = 0;
    //     for (var i = 0; i < buffer.Length; i++)
    //     {
    //         if (buffer[i] == '\r' && buffer.At(i + 1) == '\n')
    //         {
    //             i++;
    //             chIndex = 0;
    //             continue;
    //         }
    //
    //         if (buffer[0] == '\n')
    //         {
    //             chIndex = 0;
    //             continue;
    //         }
    //
    //         if (buffer[i] != '\t') continue;
    //         var charsToNextTabStop = indentation - chIndex % indentation;
    //         buffer.Remove(i, 1).Insert(i, new string(' ', charsToNextTabStop));
    //     }
    // }
}