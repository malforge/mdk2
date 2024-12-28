using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.Mod.Pack.Jobs;
using Mdk.CommandLine.Shared;
using Mdk.CommandLine.Shared.Api;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace Mdk.CommandLine.Mod.Pack;

/// <summary>
///     Processes an MDK project and produces a single script file, which is made compatible with the
///     Space Engineers Programmable Block.
/// </summary>
public class ModPacker : ProjectJob
{
    /// <summary>
    ///     Perform packing operation(s) based on the provided options.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="console"></param>
    /// <param name="interaction"></param>
    /// <exception cref="CommandLineException"></exception>
    public async Task<ImmutableArray<PackedProject>> PackAsync(Parameters parameters, IConsole console, IInteraction interaction)
    {
        if (!MSBuildLocator.IsRegistered)
        {
            var msbuildInstances = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(x => x.Version).ToArray();
            foreach (var instance in msbuildInstances)
                console.Trace($"Found MSBuild instance: {instance.Name} {instance.Version}");
            MSBuildLocator.RegisterInstance(msbuildInstances.First());
        }

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
            if (!OperatingSystem.IsWindows())
                throw new CommandLineException(-1, "The auto output option is only supported on Windows.");
            var se = new SpaceEngineers();
            var output = se.GetDataPath("Mods");
            if (string.IsNullOrEmpty(output))
                throw new CommandLineException(-1, "Failed to determine the output directory.");
            console.Trace("Output directory: " + output);
            return output;
        }

        ApplyDefaultMacros(parameters);
        parameters.DumpTrace(console);

        var filter = new FileFilter(parameters.PackVerb.Ignores, Path.GetDirectoryName(project.FilePath) ?? throw new InvalidOperationException("Project directory not set"));
        var outputPath = Path.Combine(parameters.PackVerb.Output!, project.Name);
        var outputCleanFilter = new FileFilter(parameters.PackVerb.DoNotClean, outputPath);
        var projectPath = Path.GetDirectoryName(project.FilePath)!;
        var tracePath = Path.Combine(projectPath, "obj");
        var fileSystem = new PackFileSystem(projectPath, outputPath, tracePath, console);
        var context = new PackContext(parameters, console, interaction, filter, outputCleanFilter, fileSystem, ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, parameters.PackVerb.Configuration ?? "Release"));

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
        project = await CompileAndValidateProjectAsync(project);

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

        var manager = ModProcessingManager.Create().Build();

        var processors = manager.Processors;

        var modContext = new ModPackContext(context, project, codeDocuments, contentDocuments, processors);
        modContext.Trace();

        ModJob[] jobs =
        [
            new PrepareOutputJob(),
            new CopyContentJob(),
            new ProcessScriptsJob()
        ];

        foreach (var job in jobs)
            await job.ExecuteAsync(modContext);

        return ImmutableArray<ProducedFile>.Empty.Add(new ProducedFile());
    }

    static async Task<Project> CompileAndValidateProjectAsync(Project project)
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
}