using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Api;
using Mdk.CommandLine.IngameScript.DefaultProcessors;
using Mdk.CommandLine.SharedApi;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace Mdk.CommandLine.IngameScript;

public class ScriptPacker
{
    public async Task PackAsync(PackOptions options, IConsole console)
    {
        if (!MSBuildLocator.IsRegistered) MSBuildLocator.RegisterDefaults();
        using var workspace = MSBuildWorkspace.Create();

        var projectPath = options.ProjectFile;
        if (projectPath == null) throw new CommandLineException(-1, "No project file specified.");

        if (string.Equals(Path.GetExtension(projectPath), ".csproj", StringComparison.OrdinalIgnoreCase))
        {
            console.Trace($"Packing a single project: {projectPath}");
            var project = await workspace.OpenProjectAsync(projectPath);
            if (!await PackProject(project, console))
                throw new CommandLineException(-1, "The project is not recognized as an MDK project.");

            console.Print("The project was successfully packed.");
        }
        else if (string.Equals(Path.GetExtension(projectPath), ".sln", StringComparison.OrdinalIgnoreCase))
        {
            console.Trace("Packaging a solution: " + projectPath);
            var solution = await workspace.OpenSolutionAsync(projectPath);
            var packedProjects = await PackSolution(solution, console);
            switch (packedProjects)
            {
                case 0:
                    throw new CommandLineException(-1, "No MDK projects found in the solution.");
                case 1:
                    console.Print("Successfully packed 1 project.");
                    break;
                default:
                    console.Print($"Successfully packed {packedProjects} projects.");
                    break;
            }
        }
        else
            throw new CommandLineException(-1, "Unknown file type.");
    }

    async Task<int> PackSolution(Solution solution, IConsole console) => 0;

    async Task<bool> PackProject(Project project, IConsole console)
    {
        var metadata = await ScriptProjectMetadata.LoadAsync(project);

        if (metadata == null)
            return false;

        switch (metadata.MdkProjectVersion.Major)
        {
            case < 1:
                throw new CommandLineException(-1, "The project is not recognized as an MDK project.");
            case < 2:
                console.Trace("Detected a legacy project.");
                return await PackLegacyProjectAsync(project, metadata, console);
            default:
                console.Trace("Detected a modern project.");
                return await PackProjectAsync(project, metadata, console);
        }
    }

    async Task<bool> PackProjectAsync(Project project, ScriptProjectMetadata metadata, IConsole console)
    {
        (project, var compilation) = await CompileAndValidateProjectAsync(project);

        project.TryGetDocument("instructions.readme", out var readmeDocument);
        if (readmeDocument != null)
            console.Trace("Found a readme file.");

        project.TryGetDocument("thumb.png", out var thumbnailDocument);
        if (thumbnailDocument != null)
            console.Trace("Found a thumbnail file.");

        bool isNotIgnored(Document arg)
        {
            return ShouldInclude(arg, metadata);
        }

        var allDocuments = project.Documents.Where(isNotIgnored).ToList();

        var preprocessors = await Task.WhenAll(LoadPreprocessors());
        var combiner = await LoadCombinerAsync();
        var postprocessors = await Task.WhenAll(LoadPostprocessors());
        console.Trace("There are:")
            .Trace($"  {allDocuments.Count} documents")
            .Trace($"  {preprocessors.Length} preprocessors")
            .Trace($"  {postprocessors.Length} postprocessors");

        var syntaxTrees = (await Task.WhenAll(allDocuments.Select(d => d.GetSyntaxTreeAsync()))).Cast<CSharpSyntaxTree>().ToImmutableArray();

        async Task<CSharpSyntaxTree> preprocessSyntaxTree(CSharpSyntaxTree tree)
        {
            foreach (var preprocessor in preprocessors)
                tree = await preprocessor.ProcessAsync(tree, metadata);
            return tree;
        }

        console.Trace("Preprocessing syntax trees");
        syntaxTrees = (await Task.WhenAll(syntaxTrees.Select(preprocessSyntaxTree))).ToImmutableArray();

        console.Trace("Combining syntax trees");
        var combinedSyntaxTree = await combiner.CombineAsync(syntaxTrees, metadata);

        console.Trace("Postprocessing syntax tree");
        foreach (var postprocessor in postprocessors)
            combinedSyntaxTree = await postprocessor.ProcessAsync(combinedSyntaxTree, metadata);

        console.Trace("Verifying that nothing went wrong");
        compilation = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(combinedSyntaxTree);
        var diagnostics = compilation.GetDiagnostics();
        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            foreach (var diagnostic in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                console.Print(diagnostic.ToString());
            throw new CommandLineException(-2, "Failed to compile the project.");
        }

        var final = new StringBuilder(combinedSyntaxTree.ToString());

        await OutputScriptAsync(project, console, final, readmeDocument, thumbnailDocument);

        return true;
    }

    static async Task OutputScriptAsync(Project project, IConsole console, StringBuilder script, TextDocument? readmeDocument, TextDocument? thumbnailDocument)
    {
        console.Trace("Writing the combined syntax tree to a file");
        var outputDirectory = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(project.FilePath)!, "IngameScripts", "local"));
        outputDirectory.Create();
        var outputPath = Path.Combine(outputDirectory.FullName, "script.cs");
        var buffer = new StringBuilder();
        if (readmeDocument != null)
        {
            var readmeText = await readmeDocument.GetTextAsync();
            buffer.Append("// " + string.Join("\n// ", readmeText.Lines.Select(l => l.ToString()))).Append('\n');
        }
        buffer.Append(script.ToString().Replace(Environment.NewLine, "\n"));
        await File.WriteAllTextAsync(outputPath, buffer.ToString());
        console.Trace($"The combined syntax tree was written to {outputPath}");
        if (thumbnailDocument != null)
        {
            var thumbnailPath = Path.Combine(outputDirectory.FullName, "thumb.png");
            File.Copy(thumbnailDocument.FilePath!, thumbnailPath, true);
            console.Trace($"The thumbnail was written to {thumbnailPath}");
        }
    }

    async Task<(Project, CSharpCompilation)> CompileAndValidateProjectAsync(Project project)
    {
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

        if (!diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            return (project, compilation);

        foreach (var diagnostic in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            Console.WriteLine(diagnostic);
        throw new CommandLineException(-2, "Failed to compile the project.");
    }

    static bool ShouldInclude(Document document, ScriptProjectMetadata metadata)
    {
        if (document.FilePath == null)
            return false;

        var documentFileInfo = new FileInfo(document.FilePath);
        foreach (var ignore in metadata.Ignores)
        {
            switch (ignore)
            {
                case DirectoryInfo directoryInfo:
                    if (documentFileInfo.FullName.StartsWith(directoryInfo.FullName, StringComparison.OrdinalIgnoreCase))
                        return false;
                    break;

                case FileInfo fileInfo:
                    if (string.Equals(documentFileInfo.FullName, fileInfo.FullName, StringComparison.OrdinalIgnoreCase))
                        return false;
                    break;
            }
        }

        return true;
    }

    Task<IScriptCombiner> LoadCombinerAsync() => Task.FromResult<IScriptCombiner>(new ScriptCombiner());

    IEnumerable<Task<IScriptPreprocessor>> LoadPreprocessors()
    {
        yield return Task.FromResult<IScriptPreprocessor>(new DeleteNamespaces());
    }

    IEnumerable<Task<IScriptPostprocessor>> LoadPostprocessors()
    {
        yield return Task.FromResult<IScriptPostprocessor>(new PartialMerger());
    }

    async Task<bool> PackLegacyProjectAsync(Project project, ScriptProjectMetadata metadata, IConsole console)
    {
        var root = new DirectoryInfo(Path.GetDirectoryName(project.FilePath!)!);
        metadata = metadata.WithAdditionalIgnore(new DirectoryInfo(Path.Combine(root.FullName, "obj")));

        return await PackProjectAsync(project, metadata, console);
    }
}