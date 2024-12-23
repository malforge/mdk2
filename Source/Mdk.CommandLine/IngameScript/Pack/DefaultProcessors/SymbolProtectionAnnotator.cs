using System.Linq;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Mdk.CommandLine.SharedApi;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

/// <summary>
///     Annotates the Program class and its Main and Save methods with a protected symbol annotation.
/// </summary>
[RunAfter<PartialMerger>]
public class SymbolProtectionAnnotator : IScriptPostprocessor
{
    /// <summary>
    ///     The protected symbol annotation can be used to make sure that certain symbols are not removed by any subsequent
    ///     processors (like the minifier).
    /// </summary>
    public static readonly SyntaxAnnotation ProtectedSymbolAnnotation = new("MDK", "preserve");

    /// <summary>
    ///     Annotates the Program class and its Main and Save methods with a protected symbol annotation.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    public async Task<Document> ProcessAsync(Document document, IPackContext metadata)
    {
        var root = await document.GetSyntaxRootAsync();
        if (root == null)
            return document;
        
        // Find the Program class declaration
        var programClass = root.ChildNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(c => c.Identifier.Text == "Program");
        if (programClass == null)
            return document;
        var newProgramClass = programClass.WithIdentifier(programClass.Identifier.WithAdditionalAnnotations(ProtectedSymbolAnnotation)).WithAdditionalAnnotations(ProtectedSymbolAnnotation);

        // Find and annotate the Program constructor
        var constructor = newProgramClass.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault(c => c.ParameterList.Parameters.Count == 0);
        if (constructor != null)
            newProgramClass = newProgramClass.ReplaceNode(constructor, constructor.WithIdentifier(constructor.Identifier.WithAdditionalAnnotations(ProtectedSymbolAnnotation)).WithAdditionalAnnotations(ProtectedSymbolAnnotation));
        
        // Find and annotate the Main methods
        var mainMethods = newProgramClass.Members.OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.Text == "Main").ToDictionary(m => m, m => m);
        var keys = mainMethods.Keys.ToArray();
        foreach (var mainMethod in keys)
            mainMethods[mainMethod] = mainMethod.WithIdentifier(mainMethod.Identifier.WithAdditionalAnnotations(ProtectedSymbolAnnotation)).WithAdditionalAnnotations(ProtectedSymbolAnnotation);
        newProgramClass = newProgramClass.ReplaceNodes(mainMethods.Keys, (original, _) => mainMethods[original]);

        // Find and annotate the Save methods
        var saveMethods = newProgramClass.Members.OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.Text == "Save").ToDictionary(m => m, m => m);
        keys = saveMethods.Keys.ToArray();
        foreach (var saveMethod in keys)
            saveMethods[saveMethod] = saveMethod.WithIdentifier(saveMethod.Identifier.WithAdditionalAnnotations(ProtectedSymbolAnnotation)).WithAdditionalAnnotations(ProtectedSymbolAnnotation);
        newProgramClass = newProgramClass.ReplaceNodes(saveMethods.Keys, (original, _) => saveMethods[original]);

        root = root.ReplaceNode(programClass, newProgramClass);

        return document.WithSyntaxRoot(root);
    }
}