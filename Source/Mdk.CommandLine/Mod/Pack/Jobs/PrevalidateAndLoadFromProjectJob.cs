using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mdk.CommandLine.CommandLine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mdk.CommandLine.Mod.Pack.Jobs;

internal class PrevalidateAndLoadFromProjectJob : ModJob
{
    public override async Task<ModPackContext> ExecuteAsync(ModPackContext context)
    {
        context.Console.Trace("Validating project");
        var project = context.Project;
        foreach (var document in project.Documents)
        {
            var syntaxTree = (CSharpSyntaxTree?)await document.GetSyntaxTreeAsync();
            if (syntaxTree == null)
                continue;
            var newOptions = syntaxTree.Options.WithLanguageVersion(LanguageVersion.CSharp6);
            syntaxTree = (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(syntaxTree.GetTextAsync().Result, newOptions);
            var root = await syntaxTree.GetRootAsync();
            project = document.WithSyntaxRoot(root).Project;
        }

        var compilation = await project.GetCompilationAsync() as CSharpCompilation ?? throw new CommandLineException(-1, "Failed to compile the project.");
        
        compilation = compilation.WithOptions(compilation.Options
            .WithOutputKind(OutputKind.DynamicallyLinkedLibrary)
            .WithPlatform(Platform.X64));

        var diagnostics = compilation.GetDiagnostics();

        bool isIn(IReadOnlyList<string> folders, params string[] parts)
        {
            if (folders.Count < parts.Length)
                return false;
            for (var i = 0; i < parts.Length; i++)
            {
                if (!string.Equals(folders[i], parts[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        if (!diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var existingDocument = project.GetDocument(tree);
                if (existingDocument is SourceGeneratedDocument)
                {
                    var newFilePath =  Path.Combine(context.FileSystem.ProjectPath, Path.GetFileName(tree.FilePath));
                    // var newFilePath =  Path.Combine(context.FileSystem.ProjectPath, $"{Guid.NewGuid():N}_{Path.GetFileName(tree.FilePath)}");
                    existingDocument = project.AddDocument(existingDocument.Name, await tree.GetTextAsync(), filePath: newFilePath);
                    project = existingDocument.Project;
                }
            }

            // Is there a folder named "Data/Scripts" in the project? If so we need to warn that this might cause issues.
            var path = Path.Combine(context.FileSystem.ProjectPath, "Data", "Scripts");
            if (project.AdditionalDocuments.Any(d => isIn(d.Folders, "Data", "Scripts")) || project.Documents.Any(d => isIn(d.Folders, "Data", "Scripts")))
                context.Console.Print($"{path}: Warning: A folder named 'Data/Scripts' was found in the project. This might cause issues during the packing project as files will be generated in the same location.");

            return context.WithProject(project);
        }

        foreach (var diagnostic in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            context.Console.Print(diagnostic.ToString());
        throw new CommandLineException(-2, "Failed to compile the project.");
    }
}