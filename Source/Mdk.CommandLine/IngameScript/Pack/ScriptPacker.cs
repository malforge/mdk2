using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mdk.CommandLine.Commands.Pack;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Mdk.CommandLine.SharedApi;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace Mdk.CommandLine.IngameScript.Pack;

/// <summary>
///     Processes an MDK project and produces a single script file, which is made compatible with the
///     Space Engineers Programmable Block.
/// </summary>
public class ScriptPacker
{
    /// <summary>
    ///     Perform packing operation(s) based on the provided options.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="console"></param>
    /// <param name="interaction"></param>
    /// <exception cref="CommandLineException"></exception>
    public async Task PackAsync(PackParameters options, IConsole console, IInteraction interaction)
    {
        if (!MSBuildLocator.IsRegistered)
        {
            var msbuildInstances = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(x => x.Version).ToArray();
            foreach (var instance in msbuildInstances)
                console.Trace($"Found MSBuild instance: {instance.Name} {instance.Version}");
            MSBuildLocator.RegisterInstance(msbuildInstances.First());
        }
        using var workspace = MSBuildWorkspace.Create();

        var projectPath = options.ProjectFile;
        if (projectPath == null) throw new CommandLineException(-1, "No project file specified.");

        if (string.Equals(Path.GetExtension(projectPath), ".csproj", StringComparison.OrdinalIgnoreCase))
        {
            console.Trace($"Packing a single project: {projectPath}");
            var project = await workspace.OpenProjectAsync(projectPath);
            if (!await PackProjectAsync(options, project, console, interaction))
                throw new CommandLineException(-1, "The project is not recognized as an MDK project.");

            console.Print("The project was successfully packed.");
        }
        else if (string.Equals(Path.GetExtension(projectPath), ".sln", StringComparison.OrdinalIgnoreCase))
        {
            console.Trace("Packaging a solution: " + projectPath);
            var solution = await workspace.OpenSolutionAsync(projectPath);
            var packedProjects = await PackSolutionAsync(options, solution, console, interaction);
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

    /// <summary>
    ///     Pack an entire solution.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="solution"></param>
    /// <param name="console"></param>
    /// <param name="interaction"></param>
    /// <returns></returns>
    public async Task<int> PackSolutionAsync(PackParameters options, Solution solution, IConsole console, IInteraction interaction)
    {
        var packedProjects = 0;
        foreach (var project in solution.Projects)
        {
            if (await PackProjectAsync(options, project, console, interaction))
                packedProjects++;
        }
        return packedProjects;
    }

    /// <summary>
    ///     Pack an individual project.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="project"></param>
    /// <param name="console"></param>
    /// <param name="interaction"></param>
    /// <returns></returns>
    /// <exception cref="CommandLineException"></exception>
    public async Task<bool> PackProjectAsync(PackParameters options, Project project, IConsole console, IInteraction interaction)
    {
        var metadata = ScriptProjectMetadata.ForOptions(options, new Version(2, 0, 0));
        console.Trace("Using the following metadata:")
            .Trace(metadata.ToString());

        metadata = metadata.WithAdditionalMacros(new Dictionary<string, string>
            {
                ["$MDK_DATETIME$"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                ["$MDK_DATE$"] = DateTime.Now.ToString("yyyy-MM-dd"),
                ["$MDK_TIME$"] = DateTime.Now.ToString("HH:mm")
            })
            .Close(resolveAutoOutputDirectory);

        switch (metadata.MdkProjectVersion.Major)
        {
            case < 1:
                throw new CommandLineException(-1, "The project is not recognized as an MDK project.");
            case < 2:
                console.Trace("Detected a legacy project.");
                return await PackLegacyProjectAsync(project, metadata, console, interaction);
            default:
                console.Trace("Detected a modern project.");
                return await PackProjectAsync(project, metadata, console, interaction);
        }

        string resolveAutoOutputDirectory()
        {
            console.Trace("Determining the output directory automatically...");
            if (!OperatingSystem.IsWindows())
                throw new CommandLineException(-1, "The auto output option is only supported on Windows.");
            var se = new SpaceEngineers();
            var output = se.GetDataPath("IngameScripts", "local");
            if (string.IsNullOrEmpty(output))
                throw new CommandLineException(-1, "Failed to determine the output directory.");
            console.Trace("Output directory: " + output);
            return output;
        }
    }

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    async Task<bool> PackProjectAsync(Project project, ScriptProjectMetadata metadata, IConsole console, IInteraction interaction)
    {
        var outputPath = Path.Combine(metadata.OutputDirectory, project.Name);
        var outputDirectory = new DirectoryInfo(outputPath);

        project = await CompileAndValidateProjectAsync(project);

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

        var allDocuments = project.Documents.Where(isNotIgnored).ToImmutableArray();

        if (metadata.Minify != MinifierLevel.None)
            console.Print($"Minifying is requested, but not supported yet. Ignoring the minifier level {metadata.Minify}.");
        if (metadata.TrimTypes)
            console.Print("Trimming unused types is requested, but not supported yet. Ignoring the option.");
        
        var manager = ScriptProcessingManager.Create().Build();

        var preprocessors = manager.Preprocessors;
        var combiner = manager.Combiner;
        var postprocessors = manager.Postprocessors;
        var composer = manager.Composer;
        var postCompositionProcessors = manager.PostCompositionProcessors;
        var producer = manager.Producer;

        console.Trace("There are:")
            .Trace($"  {allDocuments.Length} documents")
            .Trace($"  {preprocessors.Count} preprocessors")
            .TraceIf(preprocessors.Count > 0, $"    {string.Join("\n    ", preprocessors.Select(p => p.GetType().Name))}")
            .Trace($"  combiner {combiner.GetType().Name}")
            .Trace($"  {postprocessors.Count} postprocessors")
            .TraceIf(postprocessors.Count > 0, $"    {string.Join("\n    ", postprocessors.Select(p => p.GetType().Name))}")
            .Trace($"  composer {composer.GetType().Name}")
            .Trace($"  {postCompositionProcessors.Count} post-composition processors")
            .TraceIf(postCompositionProcessors.Count > 0, $"    {string.Join("\n    ", postCompositionProcessors.Select(p => p.GetType().Name))}")
            .Trace($"  producer {producer.GetType().Name}");

        allDocuments = await PreprocessAsync(allDocuments, preprocessors, console, metadata);
        var scriptDocument = await CombineAsync(project, combiner, allDocuments, outputDirectory, console, metadata);
        scriptDocument = await PostProcessAsync(scriptDocument, postprocessors, console, metadata);
        await VerifyAsync(console, scriptDocument);
        var final = await ComposeAsync(scriptDocument, composer, console, metadata);
        final = await PostProcessComposition(final, postCompositionProcessors, console, metadata);
        await ProduceAsync(project.Name, outputDirectory, producer, final, readmeDocument, thumbnailDocument, console, metadata);

        interaction.Notify(InteractionType.Script, null, project.Name, outputDirectory.FullName);

        return true;
    }

    async Task<Project> CompileAndValidateProjectAsync(Project project)
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
            return project;

        foreach (var diagnostic in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            Console.WriteLine(diagnostic);
        throw new CommandLineException(-2, "Failed to compile the project.");
    }

    static async Task<ImmutableArray<Document>> PreprocessAsync(ImmutableArray<Document> allDocuments, ProcessorSet<IScriptPreprocessor> preprocessors, IConsole console, ScriptProjectMetadata metadata)
    {
        async Task<Document> preprocessSyntaxTree(Document document)
        {
            foreach (var preprocessor in preprocessors)
            {
                console.Trace($"Running {nameof(preprocessor)} {preprocessor.GetType().Name} on {document.Name}");
                document = await preprocessor.ProcessAsync(document, metadata);
            }
            return document;
        }

        if (preprocessors.Count > 0)
        {
            console.Trace("Preprocessing syntax trees");
            allDocuments = (await Task.WhenAll(allDocuments.Select(preprocessSyntaxTree))).ToImmutableArray();
        }
        else
            console.Trace("No preprocessors found.");
        return allDocuments;
    }

    static async Task<Document> CombineAsync(Project project, IScriptCombiner combiner, ImmutableArray<Document> allDocuments, DirectoryInfo outputDirectory, IConsole console, ScriptProjectMetadata metadata)
    {
        console.Trace($"Running combiner {combiner.GetType().Name}");
        var scriptDocument = (await combiner.CombineAsync(project, allDocuments, metadata))
            .WithName("script.cs")
            .WithFilePath(Path.Combine(outputDirectory.FullName, "script.cs"));
        return scriptDocument;
    }

    static async Task<Document> PostProcessAsync(Document scriptDocument, ProcessorSet<IScriptPostprocessor> postprocessors, IConsole console, ScriptProjectMetadata metadata)
    {
        if (postprocessors.Count > 0)
        {
            console.Trace("Postprocessing syntax tree");
            foreach (var postprocessor in postprocessors)
            {
                console.Trace($"Running postprocessor {postprocessor.GetType().Name}");
                scriptDocument = await postprocessor.ProcessAsync(scriptDocument, metadata);
            }
        }
        else
            console.Trace("No postprocessors found.");
        return scriptDocument;
    }

    static async Task VerifyAsync(IConsole console, TextDocument scriptDocument)
    {
        console.Trace("Verifying that nothing went wrong");
        var compilation = await scriptDocument.Project.GetCSharpCompilationAsync() ?? throw new CommandLineException(-1, "Failed to compile the project.");
        var diagnostics = compilation.GetDiagnostics();
        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            foreach (var diagnostic in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                console.Print(diagnostic.ToString());
            throw new CommandLineException(-2, "Failed to compile the project.");
        }
    }

    static async Task<StringBuilder> ComposeAsync(Document scriptDocument, IScriptComposer composer, IConsole console, ScriptProjectMetadata metadata)
    {
        console.Trace($"Running composer {composer.GetType().Name}");
        var final = await composer.ComposeAsync(scriptDocument, console, metadata);
        return final;
    }

    static async Task<StringBuilder> PostProcessComposition(StringBuilder final, ProcessorSet<IScriptPostCompositionProcessor> postCompositionProcessors, IConsole console, ScriptProjectMetadata metadata)
    {
        if (postCompositionProcessors.Count > 0)
        {
            console.Trace("Post-composing the final script");
            foreach (var postCompositionProcessor in postCompositionProcessors)
            {
                console.Trace($"Running post-composition processor {postCompositionProcessor.GetType().Name}");
                final = await postCompositionProcessor.ProcessAsync(final, metadata);
            }
        }
        else
            console.Trace("No post-composition processors found.");
        return final;
    }

    static async Task ProduceAsync(string projectName, DirectoryInfo outputDirectory, IScriptProducer producer, StringBuilder final, TextDocument? readmeDocument, TextDocument? thumbnailDocument, IConsole console, ScriptProjectMetadata metadata)
    {
        console.Trace($"Running producer {producer.GetType().Name}");
        console.Trace($"Producing into {outputDirectory.FullName}");
        outputDirectory.Create();
        await producer.ProduceAsync(outputDirectory, console, final, readmeDocument, thumbnailDocument, metadata);
        // get path relative to the project
        var displayPath = Path.GetRelativePath(metadata.ProjectDirectory, outputDirectory.FullName);
        console.Print($"{projectName} => {displayPath}");
    }

    static bool ShouldInclude(Document document, ScriptProjectMetadata metadata)
    {
        if (document.FilePath == null)
            return false;
        
        return !metadata.ShouldIgnore(document.FilePath);
    }

    async Task<bool> PackLegacyProjectAsync(Project project, ScriptProjectMetadata metadata, IConsole console, IInteraction interaction)
    {
        metadata = metadata.WithAdditionalIgnore("obj/**/*");
        return await PackProjectAsync(project, metadata, console, interaction);
    }
}