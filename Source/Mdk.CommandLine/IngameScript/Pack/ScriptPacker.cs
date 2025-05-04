﻿using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Mdk.CommandLine.Shared;
using Mdk.CommandLine.Shared.Api;
using Mdk.CommandLine.Utility;
using Mdk2.Shared.Utility;
// using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace Mdk.CommandLine.IngameScript.Pack;

/// <summary>
///     Processes an MDK project and produces a single script file, which is made compatible with the
///     Space Engineers Programmable Block.
/// </summary>
public class ScriptPacker: ProjectJob
{
    const int MaxScriptLength = 100000;

    /// <summary>
    ///     Perform packing operation(s) based on the provided options.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="console"></param>
    /// <param name="interaction"></param>
    /// <exception cref="CommandLineException"></exception>
    public async Task<ImmutableArray<PackedProject>> PackAsync(Parameters parameters, IConsole console, IInteraction interaction)
    {
        // if (!MSBuildLocator.IsRegistered)
        // {
        //     var msbuildInstances = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(x => x.Version).ToArray();
        //     foreach (var instance in msbuildInstances)
        //         console.Trace($"Found MSBuild instance: {instance.Name} {instance.Version}");
        //     MSBuildLocator.RegisterInstance(msbuildInstances.First());
        // }
        using var workspace = MSBuildWorkspace.Create();

        var projectPath = parameters.PackVerb.ProjectFile;
        if (projectPath == null) throw new CommandLineException(-1, "No project file specified.");

        if (string.Equals(Path.GetExtension(projectPath), ".csproj", StringComparison.OrdinalIgnoreCase))
        {
            console.Trace($"Packing a single project: {projectPath}");
            var project = await workspace.OpenProjectAsync(projectPath);
            var result = await PackProjectAsync(parameters, project, console, interaction);
            if (!result.Any())
                throw new CommandLineException(-1, "The project is not recognized as an MDK project.");
            console.Print("The project was successfully packed.");
            return [new PackedProject(project.Name, result)];
        }
        
        if (string.Equals(Path.GetExtension(projectPath), ".sln", StringComparison.OrdinalIgnoreCase))
        {
            console.Trace("Packaging a solution: " + projectPath);
            var solution = await workspace.OpenSolutionAsync(projectPath);
            var packedProjects = await PackSolutionAsync(parameters, solution, console, interaction);
            switch (packedProjects.Length)
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
            return packedProjects;
        }
        
        throw new CommandLineException(-1, "Unknown file type.");
    }

    /// <summary>
    ///     Pack an entire solution.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="solution"></param>
    /// <param name="console"></param>
    /// <param name="interaction"></param>
    /// <returns></returns>
    public async Task<ImmutableArray<PackedProject>> PackSolutionAsync(Parameters parameters, Solution solution, IConsole console, IInteraction interaction)
    {
        var packedProjects = ImmutableArray.CreateBuilder<PackedProject>();
        foreach (var project in solution.Projects)
        {
            var result = await PackProjectAsync(parameters, project, console, interaction);
            if (result.Any())
                packedProjects.Add(new PackedProject(project.Name, result));
        }
        return packedProjects.ToImmutable();
    }

    /// <summary>
    ///     Pack an individual project.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="project"></param>
    /// <param name="console"></param>
    /// <param name="interaction"></param>
    /// <returns></returns>
    /// <exception cref="CommandLineException"></exception>
    public async Task<ImmutableArray<ProducedFile>> PackProjectAsync(Parameters parameters, Project project, IConsole console, IInteraction interaction)
    {
        if (parameters.PackVerb.Output == null || string.Equals(parameters.PackVerb.Output, "auto", StringComparison.OrdinalIgnoreCase))
            parameters.PackVerb.Output = resolveAutoOutputDirectory();
        
        string resolveAutoOutputDirectory()
        {
            console.Trace("Determining the output directory automatically...");
            var output = SpaceEngineersFactory.Create().GetDataPath();
            if (string.IsNullOrEmpty(output))
                throw new CommandLineException(-1, "Failed to determine the output directory.");
            console.Trace("Output directory: " + output);
            return output;
        }
        ApplyDefaultMacros(parameters);
        parameters.DumpTrace(console);

        var filter = new FileFilter(parameters.PackVerb.Ignores, Path.GetDirectoryName(project.FilePath) ?? throw new InvalidOperationException("Project directory not set"));
        var outputPath = Path.Combine(parameters.PackVerb.Output!, project.Name);
        var projectPath= Path.GetDirectoryName(project.FilePath)!;
        var tracePath = Path.Combine(projectPath, "obj");
        var fileSystem = new PackFileSystem(projectPath, outputPath, tracePath, console);
        var context = new PackContext(parameters, console, interaction, filter, null, fileSystem, ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, parameters.PackVerb.Configuration ?? "Release"));

        return await PackProjectAsync(project, context);
    }

    
    static void ApplyDefaultMacros(Parameters parameters)
    {
        if (!parameters.PackVerb.Macros.ContainsKey("$MDK_DATETIME$"))
            parameters.PackVerb.Macros["$MDK_DATETIME$"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        if (!parameters.PackVerb.Macros.ContainsKey("$MDK_DATE$"))
            parameters.PackVerb.Macros["$MDK_DATE$"] = DateTime.Now.ToString("yyyy-MM-dd");
        if (!parameters.PackVerb.Macros.ContainsKey("$MDK_TIME$"))
            parameters.PackVerb.Macros["$MDK_TIME$"] = DateTime.Now.ToString("HH:mm");
    }

    async Task<ImmutableArray<ProducedFile>> PackProjectAsync(Project project, PackContext context)
    {
        var outputDirectory = new DirectoryInfo(context.FileSystem.OutputDirectory);

        project = await CompileAndValidateProjectAsync(project);

        project.TryGetDocument("instructions.readme", out var readmeDocument);
        if (readmeDocument != null)
            context.Console.Trace("Found a readme file.");

        project.TryGetDocument("thumb.png", out var thumbnailDocument);
        if (thumbnailDocument != null)
            context.Console.Trace("Found a thumbnail file.");

        bool isNotIgnored(Document doc)
        {
            return context.FileFilter.IsMatch(doc.FilePath!);
        }

        // Remove ignored documents from the project
        project = project.RemoveDocuments([..project.Documents.Where(doc => !isNotIgnored(doc)).Select(doc => doc.Id)]);

        // Make sure the project is class library (convert it if necessary)
        if (project.CompilationOptions?.OutputKind != OutputKind.DynamicallyLinkedLibrary)
        {
            context.Console.Trace("Converting the project to a class library (in memory only)");
            if (project.CompilationOptions == null)
                project = project.WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            else
                project = project.WithCompilationOptions(project.CompilationOptions.WithOutputKind(OutputKind.DynamicallyLinkedLibrary));
        }
        
        var allDocuments = project.Documents.ToImmutableArray(); // project.Documents.Where(isNotIgnored).ToImmutableArray();
        
        var manager = ScriptProcessingManager.Create().Build();

        var preprocessors = manager.Preprocessors;
        var combiner = manager.Combiner;
        var postprocessors = manager.Postprocessors;
        var composer = manager.Composer;
        var postCompositionProcessors = manager.PostCompositionProcessors;
        var producer = manager.Producer;

        context.Console.Trace("There are:")
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

        // TODO: Jobify this
        allDocuments = await PreprocessAsync(allDocuments, preprocessors, context);
        var scriptDocument = await CombineAsync(project, combiner, allDocuments, context);
        scriptDocument = await PostProcessAsync(scriptDocument, postprocessors, context);

        var projectDir = Path.GetDirectoryName(project.FilePath)!;
        await VerifyAsync(context.Console, scriptDocument);
        var final = await ComposeAsync(scriptDocument, composer, context);
        final = await PostProcessComposition(final, postCompositionProcessors, context);
        var result = await ProduceAsync(projectDir, project.Name, outputDirectory, producer, final, readmeDocument, thumbnailDocument, context);

        if (final.Length > MaxScriptLength)
            context.Interaction.Custom($"NOTE: The final script has {final.Length} characters, which exceeds the maximum of {MaxScriptLength}. The programmable block will not be able to run it.");
        
        context.Interaction.Script(project.Name, outputDirectory.FullName);

        return result;
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

    static async Task<ImmutableArray<Document>> PreprocessAsync(ImmutableArray<Document> allDocuments, ProcessorSet<IDocumentProcessor> preprocessors, PackContext context)
    {
        async Task<Document> preprocessSyntaxTree(Document document)
        {
            foreach (var preprocessor in preprocessors)
            {
                context.Console.Trace($"Running {nameof(preprocessor)} {preprocessor.GetType().Name} on {document.Name}");
                document = await preprocessor.ProcessAsync(document, context);
            }
            return document;
        }

        if (preprocessors.Count > 0)
        {
            context.Console.Trace("Preprocessing syntax trees");
            allDocuments = [..await Task.WhenAll(allDocuments.Select(preprocessSyntaxTree))];
        }
        else
            context.Console.Trace("No preprocessors found.");
        return allDocuments;
    }

    static async Task<Document> CombineAsync(Project project, IScriptCombiner combiner, ImmutableArray<Document> allDocuments, PackContext context)
    {
        context.Console.Trace($"Running combiner {combiner.GetType().Name}");
        var intermediateFileName = Path.Combine(context.FileSystem.TraceDirectory, "intermediate-script.cs");
        var scriptDocument = (await combiner.CombineAsync(project, allDocuments, context))
            .WithName(Path.GetFileName(intermediateFileName))
            .WithFilePath(intermediateFileName);
        
        if (context.Console.TraceEnabled)
        {
            context.Console.Trace($"Writing intermediate script to {scriptDocument.FilePath}");
            await context.FileSystem.WriteTraceAsync(scriptDocument.FilePath!, (await scriptDocument.GetTextAsync()).ToString());
        }
        
        return scriptDocument;
    }

    static async Task<Document> PostProcessAsync(Document scriptDocument, ProcessorSet<IDocumentProcessor> postprocessors, PackContext context)
    {
        if (postprocessors.Count > 0)
        {
            context.Console.Trace("Postprocessing syntax tree");
            foreach (var postprocessor in postprocessors)
            {
                context.Console.Trace($"Running postprocessor {postprocessor.GetType().Name}");
                scriptDocument = await postprocessor.ProcessAsync(scriptDocument, context);
                scriptDocument = await scriptDocument.RemoveUnnecessaryUsingsAsync();
                if (context.Console.TraceEnabled)
                {
                    var intermediateFileName = Path.ChangeExtension(scriptDocument.FilePath!, $"{postprocessor.GetType().Name}.cs");
                    context.Console.Trace($"Writing intermediate script to {intermediateFileName}");
                    await context.FileSystem.WriteTraceAsync(intermediateFileName, (await scriptDocument.GetTextAsync()).ToString());
                }
            }
        }
        else
            context.Console.Trace("No postprocessors found.");
        return scriptDocument;
    }

    static async Task VerifyAsync(IConsole console, TextDocument scriptDocument)
    {
        console.Trace("Verifying that nothing went wrong");
        // if (console.TraceEnabled)
        // {
        //     console.Trace($"Writing intermediate script to {scriptDocument.FilePath}");
        //     var script = (await scriptDocument.GetTextAsync()).ToString();
        //     var objDirectory = Path.GetDirectoryName(scriptDocument.FilePath)!;
        //     try
        //     {
        //         Directory.CreateDirectory(objDirectory);
        //         await File.WriteAllTextAsync(scriptDocument.FilePath!, script);
        //     }
        //     catch (Exception e)
        //     {
        //         console.Print($"Failed to write intermediate script: {e.Message}");
        //     }
        // }
        var compilation = await scriptDocument.Project.GetCSharpCompilationAsync() ?? throw new CommandLineException(-1, "Failed to compile the project.");
        var diagnostics = compilation.GetDiagnostics();
        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            foreach (var diagnostic in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                console.Print(diagnostic.ToString());
            throw new CommandLineException(-2, "Failed to compile the project.");
        }
    }

    static async Task<StringBuilder> ComposeAsync(Document scriptDocument, IScriptComposer composer, PackContext context)
    {
        context.Console.Trace($"Running composer {composer.GetType().Name}");
        var final = await composer.ComposeAsync(scriptDocument, context);
        return final;
    }

    static async Task<StringBuilder> PostProcessComposition(StringBuilder final, ProcessorSet<IScriptPostCompositionProcessor> postCompositionProcessors, PackContext context)
    {
        if (postCompositionProcessors.Count > 0)
        {
            context.Console.Trace("Post-composing the final script");
            foreach (var postCompositionProcessor in postCompositionProcessors)
            {
                context.Console.Trace($"Running post-composition processor {postCompositionProcessor.GetType().Name}");
                final = await postCompositionProcessor.ProcessAsync(final, context);
            }
        }
        else
            context.Console.Trace("No post-composition processors found.");
        return final;
    }

    static async Task<ImmutableArray<ProducedFile>> ProduceAsync(string projectDirectory, string projectName, DirectoryInfo outputDirectory, IScriptProducer producer, StringBuilder final, TextDocument? readmeDocument, TextDocument? thumbnailDocument, PackContext context)
    {
        context.Console.Trace($"Running producer {producer.GetType().Name}");
        context.Console.Trace($"Producing into {outputDirectory.FullName}");
        outputDirectory.Create();
        var result = await producer.ProduceAsync(outputDirectory, final, readmeDocument, thumbnailDocument, context);
        // get path relative to the project
        var displayPath = Path.GetRelativePath(projectDirectory, outputDirectory.FullName);
        context.Console.Print($"{projectName} => {displayPath}");
        
        return result;
    }

    // static bool ShouldInclude(Document document, ScriptProjectMetadata metadata)
    // {
    //     if (document.FilePath == null)
    //         return false;
    //
    //     return !metadata.ShouldIgnore(document.FilePath);
    // }
}