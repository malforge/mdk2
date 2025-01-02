using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mdk.CommandLine.Mod.Pack.Jobs;

internal class FindAndFilterDocumentsJob : ModJob
{
    public override Task<ModPackContext> ExecuteAsync(ModPackContext context)
    {
        context.Console.Trace("Finding and filtering documents");
        var project = context.Project;

        bool documentIsNotIgnored(TextDocument doc)
        {
            return context.FileFilter.IsMatch(doc.FilePath!);
        }

        // Remove ignored documents from the project
        project = project.RemoveDocuments([..project.Documents.Where(doc => !documentIsNotIgnored(doc)).Select(doc => doc.Id)]);

        // Make sure the project is class library (convert it if necessary)
        if (project.CompilationOptions?.OutputKind != OutputKind.DynamicallyLinkedLibrary)
        {
            context.Console.Trace("Converting the project to a class library (in memory only)");
            if (project.CompilationOptions == null)
                project = project.WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            else
                project = project.WithCompilationOptions(project.CompilationOptions.WithOutputKind(OutputKind.DynamicallyLinkedLibrary));
        }

        var codeDocuments = project.Documents.Where(documentIsNotIgnored).ToImmutableArray();
        var contentDocuments = project.AdditionalDocuments.Where(documentIsNotIgnored).ToImmutableArray();

        return Task.FromResult(context
            .WithProject(project)
            .WithScriptDocuments(codeDocuments)
            .WithContentDocuments(contentDocuments)
        );
    }
}