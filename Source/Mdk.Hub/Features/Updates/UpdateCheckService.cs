using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Diagnostics;

namespace Mdk.Hub.Features.Updates;

[Singleton<IUpdateCheckService>]
public class UpdateCheckService(ILogger logger, INuGetService nuGetService, IGitHubService gitHubService, Settings.GlobalSettings globalSettings) : IUpdateCheckService
{
    readonly List<Action<VersionCheckCompletedEventArgs>> _completionCallbacks = new();
    readonly Settings.GlobalSettings _globalSettings = globalSettings;
    readonly IGitHubService _gitHubService = gitHubService;
    readonly ILogger _logger = logger;
    readonly INuGetService _nuGetService = nuGetService;
    int _isChecking; // 0 = not checking, 1 = checking

    public VersionCheckCompletedEventArgs? LastKnownVersions { get; private set; }

    public void WhenVersionCheckCompleted(Action<VersionCheckCompletedEventArgs> callback)
    {
        if (LastKnownVersions != null)
            callback(LastKnownVersions);
        else
            _completionCallbacks.Add(callback);
    }

    public async Task<bool> CheckForUpdatesAsync()
    {
        // Reentry guard
        if (Interlocked.CompareExchange(ref _isChecking, 1, 0) == 1)
        {
            _logger.Info("Update check already in progress, skipping duplicate request");
            return false;
        }

        try
        {
            _logger.Info("Starting update check...");

            var packages = await CheckNuGetPackagesAsync(CancellationToken.None);
            var templatePackage = await CheckTemplatePackageAsync(CancellationToken.None);
            var hubVersion = await CheckHubVersionAsync(CancellationToken.None);

            var results = new VersionCheckCompletedEventArgs
            {
                Packages = packages,
                TemplatePackage = templatePackage,
                HubVersion = hubVersion
            };

            LastKnownVersions = results;

            _logger.Info($"Update check completed - found {packages.Count} package versions");

            // Invoke queued callbacks
            foreach (var callback in _completionCallbacks)
                callback(results);
            _completionCallbacks.Clear();

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Update check failed: {ex.Message}");
            // Silently log errors, don't propagate
            return false;
        }
        finally
        {
            Interlocked.Exchange(ref _isChecking, 0);
        }
    }

    public async Task<bool> IsTemplateInstalledAsync()
    {
        try
        {
            _logger.Info("Checking if template package is installed");

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "new list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.Error("Failed to start dotnet process");
                return false;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Check if any of our templates appear in the output
            var hasTemplates = output.Contains("pbscript", StringComparison.OrdinalIgnoreCase) || output.Contains("modproject", StringComparison.OrdinalIgnoreCase);

            _logger.Info($"Template package installed: {hasTemplates}");
            return hasTemplates;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to check template installation: {ex.Message}");
            return false;
        }
    }

    public async Task InstallTemplateAsync()
    {
        try
        {
            _logger.Info("Installing MDK² template package");

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "new install Mal.Mdk2.ScriptTemplates",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.Error("Failed to start dotnet process");
                throw new InvalidOperationException("Failed to start dotnet process");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.Error($"Template installation failed: {error}");
                throw new InvalidOperationException($"Template installation failed: {error}");
            }

            _logger.Info("Template package installed successfully");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to install template: {ex.Message}");
            throw;
        }
    }

    public async Task<(bool IsInstalled, string? Version)> CheckDotNetSdkAsync()
    {
        try
        {
            _logger.Info("Checking .NET SDK installation");

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.Info(".NET SDK not found");
                return (false, null);
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                var version = output.Trim();
                _logger.Info($".NET SDK found: {version}");
                return (true, version);
            }

            _logger.Info(".NET SDK not found");
            return (false, null);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to check .NET SDK: {ex.Message}");
            return (false, null);
        }
    }

    public async Task InstallDotNetSdkAsync()
    {
        try
        {
            _logger.Info("Installing .NET SDK");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: Download and run the installer
                var installerUrl = "https://download.visualstudio.microsoft.com/download/pr/23e32323-5a2b-48c2-86fa-58f5e72c6e98/19e09b4411d771867c0c9c30a8d7062c/dotnet-sdk-9.0.101-win-x64.exe";
                var installerPath = Path.Combine(Path.GetTempPath(), "dotnet-sdk-9-installer.exe");

                _logger.Info("Downloading .NET 9 SDK installer");

                using var httpClient = new HttpClient();
                var installerBytes = await httpClient.GetByteArrayAsync(installerUrl);
                await File.WriteAllBytesAsync(installerPath, installerBytes);

                _logger.Info($"Running installer at {installerPath}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = "/quiet /norestart",
                    UseShellExecute = true,
                    Verb = "runas" // Request elevation
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                    throw new InvalidOperationException("Failed to start installer");

                await process.WaitForExitAsync();

                if (process.ExitCode != 0 && process.ExitCode != 3010) // 3010 = success but reboot required
                    throw new InvalidOperationException($"Installer failed with exit code {process.ExitCode}");

                _logger.Info(".NET SDK installed successfully");

                // Clean up installer
                try
                {
                    File.Delete(installerPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux: Use the dotnet-install.sh script
                var scriptUrl = "https://dot.net/v1/dotnet-install.sh";
                var scriptPath = Path.Combine(Path.GetTempPath(), "dotnet-install.sh");

                _logger.Info("Downloading .NET install script");

                using var httpClient = new HttpClient();
                var scriptBytes = await httpClient.GetByteArrayAsync(scriptUrl);
                await File.WriteAllBytesAsync(scriptPath, scriptBytes);

                _logger.Info("Making script executable");

                var chmodInfo = new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x {scriptPath}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var chmodProcess = Process.Start(chmodInfo);
                await chmodProcess!.WaitForExitAsync();

                _logger.Info("Running install script");

                var startInfo = new ProcessStartInfo
                {
                    FileName = scriptPath,
                    Arguments = "--channel 9.0 --install-dir $HOME/.dotnet",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                    throw new InvalidOperationException("Failed to start installer");

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.Error($"Installer output: {output}");
                    _logger.Error($"Installer error: {error}");
                    throw new InvalidOperationException($"Installer failed with exit code {process.ExitCode}");
                }

                _logger.Info(".NET SDK installed successfully to $HOME/.dotnet");
                _logger.Info("Note: User may need to add $HOME/.dotnet to PATH");

                // Clean up script
                try
                {
                    File.Delete(scriptPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            else
                throw new PlatformNotSupportedException("Automatic SDK installation not supported on this platform");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to install .NET SDK: {ex.Message}");
            throw;
        }
    }

    async Task<IReadOnlyList<PackageVersionInfo>> CheckNuGetPackagesAsync(CancellationToken cancellationToken)
    {
        _logger.Info("Checking NuGet packages for updates");

        var packages = new[]
        {
            "Mal.Mdk2.PbAnalyzers",
            "Mal.Mdk2.PbPackager",
            "Mal.Mdk2.ModAnalyzers",
            "Mal.Mdk2.ModPackager",
            "Mal.Mdk2.References"
        };

        var results = new List<PackageVersionInfo>();

        foreach (var packageId in packages)
        {
            var version = await _nuGetService.GetLatestVersionAsync(packageId, cancellationToken);
            if (version != null)
            {
                results.Add(new PackageVersionInfo
                {
                    PackageId = packageId,
                    LatestVersion = version
                });
            }
        }

        return results;
    }

    async Task<TemplateVersionInfo?> CheckTemplatePackageAsync(CancellationToken cancellationToken)
    {
        _logger.Info("Checking template package for updates");

        var version = await _nuGetService.GetLatestVersionAsync("Mal.Mdk2.ScriptTemplates", cancellationToken);
        if (version != null)
        {
            return new TemplateVersionInfo
            {
                LatestVersion = version
            };
        }

        return null;
    }

    async Task<HubVersionInfo?> CheckHubVersionAsync(CancellationToken cancellationToken)
    {
        _logger.Info("Checking Hub version for updates");

        var includePrerelease = _globalSettings.IncludePrereleaseUpdates;
        var version = await _gitHubService.GetLatestReleaseAsync("malforge", "mdk2", includePrerelease, cancellationToken);
        if (version != null)
        {
            return new HubVersionInfo
            {
                LatestVersion = version.Value.Version,
                IsPrerelease = version.Value.IsPrerelease,
                DownloadUrl = "https://github.com/malforge/mdk2/releases/latest"
            };
        }

        return null;
    }
}