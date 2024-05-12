using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Mdk.CommandLine;

/// <summary>
///     Utilities for working with <see cref="Project" />.
/// </summary>
public static class ProjectExtensions
{
    /// <summary>
    ///     Attempts to get a document from the project with the specified path.
    /// </summary>
    /// <remarks>
    ///     Specify the path relative to the project file.
    /// </remarks>
    /// <param name="project"></param>
    /// <param name="documentPath"></param>
    /// <param name="document"></param>
    /// <returns></returns>
    public static bool TryGetDocument(this Project project, string documentPath, [MaybeNullWhen(false)] out TextDocument document)
    {
        var projectPath = Path.GetDirectoryName(project.FilePath);
        
        bool isMatch(TextDocument d)
        {
            return d.FilePath != null && string.Equals(projectPath == null ? d.FilePath : Path.GetRelativePath(projectPath, d.FilePath), documentPath, StringComparison.OrdinalIgnoreCase);
        }
        
        document = project.Documents.FirstOrDefault(isMatch) ?? project.AdditionalDocuments.FirstOrDefault(isMatch);
        return document != null;
    }
    
    /// <summary>
    ///     Attempts to get a document from the project with the specified file name.
    /// </summary>
    /// <remarks>
    ///     Do not specify the path, just the file name. Will return the first document with a matching file name
    /// </remarks>
    /// <param name="project"></param>
    /// <param name="fileName"></param>
    /// <param name="document"></param>
    /// <returns></returns>
    public static bool TryFindDocument(this Project project, string fileName, [MaybeNullWhen(false)] out TextDocument document)
    {
        bool isMatch(TextDocument d)
        {
            return d.FilePath != null && string.Equals(Path.GetFileName(d.FilePath), fileName, StringComparison.OrdinalIgnoreCase);
        }
        
        document = project.Documents.FirstOrDefault(isMatch) ?? project.AdditionalDocuments.FirstOrDefault(isMatch);
        return document != null;
    }
    
    /// <summary>
    ///     Gets the <see cref="CSharpCompilation" /> for the project.
    /// </summary>
    /// <param name="project"></param>
    /// <returns></returns>
    public static async Task<CSharpCompilation?> GetCSharpCompilationAsync(this Project project) => (CSharpCompilation?)await project.GetCompilationAsync();
    
    /// <summary>
    ///     Removes unnecessary using directives from the document.
    /// </summary>
    public static async Task<Document> RemoveUnnecessaryUsingsAsync(this Document document)
    {
        // Simulate a build to get diagnostics
        var compilation = await document.Project.GetCompilationAsync();
        if (compilation == null)
            throw new InvalidOperationException("Failed to get compilation.");
        var diagnostics = compilation.GetDiagnostics();
        
        // Filter for CS0246 errors
        var invalidNamespaceDiagnostics = diagnostics.Where(d => d.Id == "CS0246");
        
        var root = await document.GetSyntaxRootAsync();
        if (root == null)
            throw new InvalidOperationException("Failed to get syntax root.");
        var usingDirectivesToRemove = invalidNamespaceDiagnostics
            .SelectMany(d => root.FindToken(d.Location.SourceSpan.Start).Parent!.AncestorsAndSelf().OfType<UsingDirectiveSyntax>())
            .Distinct();
        
        // Remove the using directives
        var newRoot = root.RemoveNodes(usingDirectivesToRemove, SyntaxRemoveOptions.KeepNoTrivia);
        if (newRoot == null)
            throw new InvalidOperationException("Failed to remove unnecessary using directives.");
        var formattedRoot = Formatter.Format(newRoot, Formatter.Annotation, document.Project.Solution.Workspace);
        return document.WithSyntaxRoot(formattedRoot);

        // var root = await document.GetSyntaxRootAsync();
        // if (root == null)
        //     return document;
        //
        // var model = await document.GetSemanticModelAsync();
        // if (model == null)
        //     return document;
        //
        // var usingDirectives = root.DescendantNodes()
        //     .OfType<UsingDirectiveSyntax>()
        //     .ToList();
        //
        // var unnecessaryUsings = usingDirectives.Where(u => IsUsingDirectiveUnnecessary(u, model));
        //
        // var newRoot = root.RemoveNodes(unnecessaryUsings, SyntaxRemoveOptions.KeepNoTrivia);
        // if (newRoot == null)
        //     throw new InvalidOperationException("Failed to remove unnecessary using directives.");
        //
        // var formattedRoot = Formatter.Format(newRoot, Formatter.Annotation, document.Project.Solution.Workspace);
        // return document.WithSyntaxRoot(formattedRoot);
    }
    
    // static bool IsUsingDirectiveUnnecessary(UsingDirectiveSyntax usingDirective, SemanticModel model)
    // {
    //     if (usingDirective.Name is null)
    //         return false;
    //     
    //     var namespaceName = usingDirective.Name.ToString();
    //     var symbols = model.LookupNamespacesAndTypes(usingDirective.SpanStart, name: namespaceName);
    //     return !symbols.Any();
    // }
}