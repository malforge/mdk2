using System.Linq;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Mdk.CommandLine.IngameScript.DefaultProcessors;

[RunAfter<PartialMerger>]
public class TypeSorter: IScriptPostprocessor
{
    public async Task<Document> ProcessAsync(Document document, ScriptProjectMetadata metadata)
    {
        var root = await document.GetSyntaxRootAsync();
        
        if (root is null)
            return document;
        
        var editor = await DocumentEditor.CreateAsync(document);
        root = editor.GetChangedRoot();

        var typeDeclarations = root.ChildNodes().OfType<TypeDeclarationSyntax>().ToList();
        if (typeDeclarations.FirstOrDefault()?.Identifier.Text == "Program")
            return document;
        var programType = typeDeclarations.FirstOrDefault(t => t.Identifier.Text == "Program");
        if (programType is null)
            return document;

        typeDeclarations.Remove(programType);
        editor.RemoveNode(programType);
        editor.InsertBefore(typeDeclarations.First(), programType);
        
        return document.WithSyntaxRoot(editor.GetChangedRoot());
    }
}