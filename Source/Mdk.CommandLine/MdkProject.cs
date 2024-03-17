using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mdk.CommandLine.SharedApi;
using Mdk.CommandLine.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Mdk.CommandLine;

public readonly struct MdkProject
{
    public readonly MSBuildWorkspace Workspace;
    public readonly Project Project;
    public readonly MdkProjectType Type;

    MdkProject(MSBuildWorkspace workspace, Project project, MdkProjectType type)
    {
        Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        Project = project ?? throw new ArgumentNullException(nameof(project));
        Type = type != MdkProjectType.Unknown ? type : throw new ArgumentOutOfRangeException(nameof(type));
    }

    public static async IAsyncEnumerable<MdkProject> LoadAsync(string fileName, IConsole console)
    {
        if (fileName == null) throw new CommandLineException(-1, "No project file specified.");

        MsBuild.Install(console);

        using var workspace = MSBuildWorkspace.Create();
        if (string.Equals(Path.GetExtension(fileName), ".csproj", StringComparison.OrdinalIgnoreCase))
        {
            var project = await workspace.OpenProjectAsync(fileName);
            var type = Identify(project, console);
            if (type != MdkProjectType.Unknown)
                yield return new MdkProject(workspace, project, type);
        }
        else if (string.Equals(Path.GetExtension(fileName), ".sln", StringComparison.OrdinalIgnoreCase))
        {
            var solution = await workspace.OpenSolutionAsync(fileName);
            foreach (var project in solution.Projects)
            {
                var type = Identify(project, console);
                if (type != MdkProjectType.Unknown)
                    yield return new MdkProject(workspace, project, type);
            }
        }
    }

    static MdkProjectType Identify(Project project, IConsole console)
    {
        // If there are ini files named {projectName].mdk.ini, then it's an MDK2 project, we'll have to read it to know if it's a mod or an ingame script.
        var projectFileName = Path.GetFileName(project.FilePath);
        var iniFileName = Path.ChangeExtension(project.FilePath, ".mdk.ini");
        if (File.Exists(iniFileName))
        {
            try
            {
                var ini = Ini.FromFile(iniFileName);
                var type = ini["mdk"]["type"].ToEnum<MdkProjectType>();
                if (type == MdkProjectType.Unknown)
                {
                    console.Print($"The project file {projectFileName} seems to be an MDK2 project, but the .mdk.ini file does not define the project type.");
                    return MdkProjectType.Unknown;
                }
                return type;
            }
            catch (Exception e)
            {
                console.Print($"The project file {projectFileName} seems to be an MDK2 project, but the .mdk.ini file is invalid: {e.Message}");
                return MdkProjectType.Unknown;
            }
        }

        // If there are no ini files, we need to see if the project contains the "mdk.options.props" file, which is a legacy way of identifying MDK projects.
        var propsFileName = Path.Combine(Path.GetDirectoryName(project.FilePath) ?? throw new InvalidOperationException("The project file path is invalid."), "mdk", "mdk.options.props");
        var mdkPropsDocument = project.AdditionalDocuments.FirstOrDefault(d => string.Equals(d.FilePath, propsFileName, StringComparison.OrdinalIgnoreCase));
        return mdkPropsDocument != null ? MdkProjectType.LegacyProgrammableBlock : MdkProjectType.Unknown;
    }
}