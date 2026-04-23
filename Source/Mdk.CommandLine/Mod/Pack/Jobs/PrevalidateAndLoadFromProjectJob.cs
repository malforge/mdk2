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
            // Collect source-generated documents and inline them as regular documents with physical file
            // paths so that ProcessScriptsJob can write them to the output directory. The generators are
            // stripped from the project to prevent duplicate type definitions on subsequent compilations.
            var sourceGeneratedDocuments = (await project.GetSourceGeneratedDocumentsAsync()).ToImmutableArray();
            if (sourceGeneratedDocuments.Length > 0)
            {
                context.Console.Trace($"  {sourceGeneratedDocuments.Length} source-generated documents found");
                foreach (var analyzerRef in project.AnalyzerReferences.ToArray())
                {
                    if (analyzerRef.GetGenerators(LanguageNames.CSharp).Any())
                        project = project.RemoveAnalyzerReference(analyzerRef);
                }
                foreach (var sgDoc in sourceGeneratedDocuments)
                {
                    var text = await sgDoc.GetTextAsync();
                    var newFilePath = Path.Combine(context.FileSystem.ProjectPath, sgDoc.HintName);
                    project = project.AddDocument(sgDoc.HintName, text, filePath: newFilePath).Project;
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