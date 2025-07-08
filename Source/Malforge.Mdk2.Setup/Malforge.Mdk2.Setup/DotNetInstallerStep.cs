using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Malforge.Mdk2.Setup.Foundation;
using Semver;

namespace Malforge.Mdk2.Setup;

public class DotNetInstallerStep() : InstallerStep("DotNet 9")
{
    public override async Task RunAsync(CancellationToken cancellationToken = default)
    {
        CurrentOperation = "Checking .NET installation...";
        var version = await GetDotnetVersionAsync(cancellationToken);
        if (version?.Major >= 9)
        {
            CurrentOperation = "Success.";
            Progress = 1.0f;
            return;
        }
        
        CurrentOperation = "Determining .NET installer source...";
        var downloads = await DotNetDownloads.CreateAsync("9.0", cancellationToken);
    }

    async Task<SemVersion?> GetDotnetVersionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var psi = new ProcessStartInfo("dotnet", "--version")
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

            var versionString = proc.StandardOutput.ReadLine()?.Trim();
            if (!SemVersion.TryParse(versionString, out var semVer) || semVer.Major != 9)
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
}