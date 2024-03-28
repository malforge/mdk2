using System;
using System.IO;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.SharedApi;
using Mdk.CommandLine.Utility;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.IngameScript;

/// <summary>
/// Base class for jobs working on MDK projects.
/// </summary>
public abstract class ProjectJob
{
    /// <summary>
    /// Attempt to update the parameters from the project configuration files.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="project"></param>
    /// <param name="console"></param>
    /// <exception cref="CommandLineException"></exception>
    protected static void UpdateParametersFromConfig(Parameters parameters, Project project, IConsole console)
    {
        var iniFileName = Path.ChangeExtension(project.FilePath, ".mdk.ini");
        var localIniFileName = Path.ChangeExtension(project.FilePath, ".mdk.local.ini");

        if (File.Exists(iniFileName))
        {
            console.Trace($"Found an MDK project configuration file: {iniFileName}");
            var ini = Ini.FromFile(iniFileName);
            parameters.Load(ini);
            parameters.DumpTrace(console);
        }

        if (File.Exists(localIniFileName))
        {
            console.Trace($"Found a local MDK project configuration file: {localIniFileName}");
            var ini = Ini.FromFile(localIniFileName);
            parameters.Load(ini);
            parameters.DumpTrace(console);
        }

        if (parameters.PackVerb.Output == null || string.Equals(parameters.PackVerb.Output, "auto", StringComparison.OrdinalIgnoreCase))
            parameters.PackVerb.Output = resolveAutoOutputDirectory();

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
}