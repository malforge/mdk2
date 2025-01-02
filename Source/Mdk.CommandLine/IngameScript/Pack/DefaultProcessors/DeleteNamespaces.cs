using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Mdk.CommandLine.Shared.Api;
using Mdk.CommandLine.Shared.DefaultProcessors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

/// <summary>
///     Removes all namespaces from the syntax tree.
/// </summary>
/// <remarks>
///     Programmable block scripts do not support namespaces, so this preprocessor removes them.
///     Note: Will also convert tabs to spaces and unindent the code.
/// </remarks>
[RunAfter<PreprocessorConditionals>]
public class DeleteNamespaces : IDocumentProcessor
{
    // The default indent size. _Maybe_ we will make this configurable in the future.
    const int IndentSize = 4;

    /// <inheritdoc />
    public async Task<Document> ProcessAsync(Document document, IPackContext metadata)
    {
        var syntaxTree = (CSharpSyntaxTree?)await document.GetSyntaxTreeAsync();
        if (syntaxTree == null)
            return document;
        CSharpSyntaxNode root = await syntaxTree.GetRootAsync(), originalRoot = root;
        var namespaceDeclarations = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().ToArray();
        while (namespaceDeclarations.Length > 0)
        {
            var current = namespaceDeclarations[0];

            var unindentedMembers = await Task.WhenAll(current.Members.Select(m => UnindentAsync(m, IndentSize)));

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
        buffer.ConvertTabsToSpaces(indentation)
            .Unindent(indentation);

        return (MemberDeclarationSyntax)(await CSharpSyntaxTree.ParseText(buffer.ToString()).GetRootAsync()).ChildNodes().First();
    }
}