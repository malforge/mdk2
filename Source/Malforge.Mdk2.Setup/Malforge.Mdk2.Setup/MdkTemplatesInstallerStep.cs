using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Malforge.Mdk2.Setup.Foundation;
using Semver;

namespace Malforge.Mdk2.Setup;

public partial class MdkTemplatesInstallerStep() : InstallerStep("Mdk² Templates")
{
    public override async Task RunAsync(CancellationToken cancellationToken = default)
    {
        CurrentOperation = "Checking Mdk² templates installation...";
        var version = await GetMdkTemplatesVersionAsync(cancellationToken);
        var scriptTemplatesVersion = await Nuget.GetPackageVersionAsync(
            "Mal.Mdk2.ScriptTemplates",
            cancellationToken: cancellationToken
        );
        // If version is null or older than the script templates version, we need to install or update
        if (version == null || version.CompareSortOrderTo(scriptTemplatesVersion) < 0)
        {
            CurrentOperation = "Installing Mdk² templates...";
            Progress = 0.0f;

            // run dotnet to install the templates
            var psi = new ProcessStartInfo("dotnet", $"new -i Mal.Mdk2.ScriptTemplates::{scriptTemplatesVersion}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null)
                throw new InvalidOperationException("Failed to start dotnet process for installing Mdk² templates.");

            await proc.WaitForExitAsync(cancellationToken);
            if (proc.ExitCode != 0)
            {
                CurrentOperation = "Failed to install Mdk² templates.";
                throw new InvalidOperationException($"Failed to install Mdk² templates: {await proc.StandardError.ReadToEndAsync(cancellationToken)}");
            }
        }

        CurrentOperation = "Success.";
        Progress = 1.0f;
    }


    async Task<SemVersion?> GetMdkTemplatesVersionAsync(CancellationToken cancellationToken = default)
    {
        // run dotnet to check if Mdk² templates are installed, and if so, return the version
        try
        {
            var psi = new ProcessStartInfo("dotnet", "new --list")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null)
                return null;
            await proc.WaitForExitAsync(cancellationToken);
            if (proc.ExitCode != 0)
                return null;

            var output = await proc.StandardOutput.ReadToEndAsync(cancellationToken);
            var match = VersionRegex().Match(output);
            if (!match.Success)
                return null;
            var versionString = match.Groups["version"].Value.Trim();
            if (!SemVersion.TryParse(versionString, out var semVer) || semVer.Major != 2)
                return null;
            return semVer;
        }
        catch (Win32Exception)
        {
            // “dotnet” command not found
            return null;
        }
        catch (FileNotFoundException)
        {
            // “dotnet” command not found
            return null;
        }
    }

    [GeneratedRegex(@"^Mal\.Mdk2\.ScriptTemplates\s*$\s*Version:\s*(?<version>[^\s]+)", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex VersionRegex();
}