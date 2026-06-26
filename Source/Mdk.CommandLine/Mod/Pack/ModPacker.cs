using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.Mod.Pack.Jobs;
using Mdk.CommandLine.Shared;
using Mdk.CommandLine.Shared.Api;
using Mdk.CommandLine.Utility;
// using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
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
        // if (!MSBuildLocator.IsRegistered)
        // {
        //     var msbuildInstances = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(x => x.Version).ToArray();
        //     foreach (var instance in msbuildInstances)
        //         console.Trace($"Found MSBuild instance: {instance.Name} {instance.Version}");
        //     MSBuildLocator.RegisterInstance(msbuildInstances.First());
        // }

        var workspaceProperties = new Dictionary<string, string>
        {
            ["Configuration"] = parameters.PackVerb.Configuration ?? "Release"
        };
        using var workspace = MSBuildWorkspace.Create(workspaceProperties);

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

        if (string.Equals(Path.GetExtension(projectPath), ".sln", StringComparison.OrdinalIgnoreCase)
         || string.Equals(Path.GetExtension(projectPath), ".slnx", StringComparison.OrdinalIgnoreCase))
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
            
            // Check for custom global setting first
            var customPath = Shared.GlobalSettings.GetCustomAutoModOutputPath();
            if (customPath != null)
            {
                console.Trace($"Using custom auto mod output path from global settings: {customPath}");
                return customPath;
            }
            
            // Fall back to default behavior
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

        var projectPath = Path.GetDirectoryName(project.FilePath) ?? throw new InvalidOperationException("Project directory not set");
        var branch = GitBranchResolver.GetCurrentBranch(projectPath);
        parameters.PackVerb.Macros["$MDK_PROJECT$"] = project.Name;
        parameters.PackVerb.Macros["$MDK_BRANCH$"] = branch ?? string.Empty;

        parameters.DumpTrace(console);

        var outputLeaf = ResolveOutputLeaf(project.Name, branch, parameters.PackVerb.BranchOutputs, parameters.PackVerb.Macros);
        if (!string.Equals(outputLeaf, project.Name, StringComparison.Ordinal))
            console.Trace($"Branch '{branch}' redirects output to folder '{outputLeaf}' (instead of '{project.Name}').");

        var filter = new FileFilter(parameters.PackVerb.Ignores, projectPath);
        var outputPath = Path.Combine(parameters.PackVerb.Output!, outputLeaf);
        var outputCleanFilter = new FileFilter(parameters.PackVerb.DoNotClean, outputPath);
        var tracePath = Path.Combine(projectPath, "obj");
        var fileSystem = new PackFileSystem(projectPath, outputPath, tracePath, console);
        var context = new ModPackContext(parameters, console, interaction, filter, outputCleanFilter, fileSystem, PackContext.ResolvePreprocessorSymbols(project), project, default, default, default);

        return await PackProjectAsync(context);
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

    /// <summary>
    ///     Determines the output folder leaf name for a mod project, honoring any <c>[mdk-branch:*]</c>
    ///     rules. Returns <paramref name="projectName" /> unless the current branch has a configured pattern.
    /// </summary>
    /// <param name="projectName">The project name, used as the default folder name.</param>
    /// <param name="branch">The current git branch, or <c>null</c> if it could not be determined.</param>
    /// <param name="branchPatterns">Branch-name to folder-pattern map from the ini.</param>
    /// <param name="macros">Macros to substitute into the chosen pattern (must include $MDK_PROJECT$/$MDK_BRANCH$).</param>
    /// <exception cref="CommandLineException">
    ///     Thrown when branch patterns are configured but the current branch cannot be determined, to avoid
    ///     silently packing to the wrong (released) mod folder.
    /// </exception>
    public static string ResolveOutputLeaf(string projectName, string? branch, IReadOnlyDictionary<string, BranchOutput> branchOutputs, IReadOnlyDictionary<string, string> macros)
    {
        if (branchOutputs.Count == 0)
            return projectName;

        if (branch == null)
            throw new CommandLineException(-1, "Branch-specific output folders ([mdk-branch:...]) are configured, but the current git branch could not be determined (no git repository, or a detached HEAD). Aborting to avoid packing to the wrong mod folder.");

        if (!branchOutputs.TryGetValue(branch, out var config) || string.IsNullOrWhiteSpace(config.Pattern))
            return projectName;

        var leaf = SanitizeFolderName(MacroReplacer.Replace(config.Pattern, macros));
        return string.IsNullOrWhiteSpace(leaf) ? projectName : leaf;
    }

    /// <summary>
    ///     Replaces any characters that are not valid in a file/folder name (e.g. a slash from a branch name
    ///     used via $MDK_BRANCH$) with a hyphen, so the result is always a single folder leaf.
    /// </summary>
    static string SanitizeFolderName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new System.Text.StringBuilder(name.Length);
        foreach (var c in name)
            builder.Append(Array.IndexOf(invalid, c) >= 0 ? '-' : c);
        return builder.ToString().Trim();
    }

    async Task<ImmutableArray<ProducedFile>> PackProjectAsync(ModPackContext context)
    {
        ModJob[] jobs =
        [
            new PrevalidateAndLoadFromProjectJob(),
            new TrimAttributesJob(),
            new FindAndFilterDocumentsJob(),
            new LoadProcessorsJob(),
            new PrepareOutputJob(),
            new CopyContentJob(),
            new ProcessScriptsJob()
        ];

        foreach (var job in jobs)
            context = await job.ExecuteAsync(context);

        var project = context.Project;
        if (!string.IsNullOrEmpty(project.FilePath))
        {
            // Report the actual deployed folder name so the Hub reflects branch-redirected builds
            // (e.g. "MyMod.Alpha") rather than always the project name.
            var deployedName = Path.GetFileName(context.FileSystem.OutputDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrEmpty(deployedName))
                deployedName = project.Name;
            context.Interaction.Mod(deployedName, project.FilePath);
        }

        return ImmutableArray<ProducedFile>.Empty.Add(new ProducedFile());
    }
}
