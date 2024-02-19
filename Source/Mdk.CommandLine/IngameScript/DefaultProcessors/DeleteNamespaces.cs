using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mdk.CommandLine.IngameScript.DefaultProcessors;

/// <summary>
///     Removes all namespaces from the syntax tree.
/// </summary>
/// <remarks>
///     Programmable block scripts do not support namespaces, so this preprocessor removes them.
///     Note: Will also convert tabs to spaces and unindent the code.
/// </remarks>
public class DeleteNamespaces : IScriptPreprocessor
{
    /// <inheritdoc />
    public async Task<Document> ProcessAsync(Document document, ScriptProjectMetadata metadata)
    {
        var syntaxTree = (CSharpSyntaxTree?)await document.GetSyntaxTreeAsync();
        if (syntaxTree == null)
            return document;
        CSharpSyntaxNode root = await syntaxTree.GetRootAsync(), originalRoot = root;
        var namespaceDeclarations = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().ToArray();
        while (namespaceDeclarations.Length > 0)
        {
            var current = namespaceDeclarations[0];

            var unindentedMembers = await Task.WhenAll(current.Members.Select(m => UnindentAsync(m, metadata.IndentSize)));

            var newRoot = root.ReplaceNode(current, unindentedMembers);
            root = newRoot;
            namespaceDeclarations = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().ToArray();
        }

        return root == originalRoot ? document : document.WithSyntaxRoot(root);
    }

    static async Task<MemberDeclarationSyntax> UnindentAsync(SyntaxNode typeDeclaration, int indentation)
    {
        var text = await typeDeclaration.SyntaxTree.GetTextAsync();
        var buffer = new StringBuilder((int)(text.Length * 1.5));
        var span = typeDeclaration.Span;

        var startOfLine = span.Start;
        while (startOfLine > 0 && text[startOfLine - 1] != '\n' && char.IsWhiteSpace(text[startOfLine - 1]))
            startOfLine--;

        var endOfLine = span.End;
        while (endOfLine < text.Length && text[endOfLine] != '\n' && char.IsWhiteSpace(text[endOfLine]))
            endOfLine++;
        
        var needsEndOfLine = endOfLine < text.Length && text[endOfLine] == '\n';

        var alteredSpan = new TextSpan(startOfLine, endOfLine - startOfLine);
        
        buffer.Append(text.ToString(alteredSpan).TrimEnd());
        if (needsEndOfLine)
            buffer.Append('\n');
        ConvertTabsToSpaces(buffer, indentation);
        Unindent(buffer, indentation);

        return (MemberDeclarationSyntax)(await CSharpSyntaxTree.ParseText(buffer.ToString()).GetRootAsync()).ChildNodes().First();
    }

    static void Unindent(StringBuilder buffer, int indentation)
    {
        var indentString = new string(' ', indentation);
        if (!IsIndented(buffer, indentString))
            return;

        var startOfLine = 0;
        var endOfLine = buffer.FindNewLine(0);
        while (endOfLine != -1)
        {
            if (IsLineWhitespace(buffer, startOfLine, endOfLine))
            {
                startOfLine = buffer.SkipNewLine(endOfLine);
                endOfLine = buffer.FindNewLine(startOfLine);
                continue;
            }

            if (buffer.StartsWith(startOfLine, indentString))
            {
                buffer.Remove(startOfLine, indentation);
                startOfLine = buffer.SkipNewLine(endOfLine - indentation);
                endOfLine = buffer.FindNewLine(startOfLine);
                continue;
            }

            startOfLine = buffer.FindNewLine(endOfLine);
            endOfLine = buffer.FindNewLine(startOfLine);
        }
    }

    static bool IsIndented(StringBuilder buffer, string indentString)
    {
        var startOfLine = 0;
        var endOfLine = buffer.FindNewLine(0);
        while (endOfLine != -1)
        {
            if (IsLineWhitespace(buffer, startOfLine, endOfLine))
            {
                startOfLine = buffer.SkipNewLine(endOfLine);
                endOfLine = buffer.FindNewLine(startOfLine);
                continue;
            }

            if (!buffer.StartsWith(startOfLine, indentString))
                return false;
            startOfLine = buffer.SkipNewLine(endOfLine);
            endOfLine = buffer.FindNewLine(startOfLine);
        }

        return true;
    }

    static bool IsLineWhitespace(StringBuilder buffer, int startOfLine, int endOfLine)
    {
        for (var i = startOfLine; i < endOfLine; i++)
        {
            if (!char.IsWhiteSpace(buffer[i]))
                return false;
        }
        return true;
    }

    static void ConvertTabsToSpaces(StringBuilder buffer, int indentation)
    {
        var chIndex = 0;
        for (var i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] == '\r' && buffer.At(i + 1) == '\n')
            {
                i++;
                chIndex = 0;
                continue;
            }

            if (buffer[0] == '\n')
            {
                chIndex = 0;
                continue;
            }

            if (buffer[i] != '\t') continue;
            var charsToNextTabStop = indentation - chIndex % indentation;
            buffer.Remove(i, 1).Insert(i, new string(' ', charsToNextTabStop));
        }
    }
}