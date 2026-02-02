using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Settings;
using Mdk.Hub.Features.Shell;
using NuGet.Versioning;

namespace Mdk.Hub.Features.Updates;

[Singleton<IUpdateCheckService>]
public class UpdateCheckService : IUpdateCheckService
{
    readonly List<Action<VersionCheckCompletedEventArgs>> _completionCallbacks = new();
    readonly IGitHubService _gitHubService;
    readonly ILogger _logger;
    readonly INuGetService _nuGetService;
    readonly ISettings _settings;
    readonly IShell _shell;
    int _isChecking; // 0 = not checking, 1 = checking

    public UpdateCheckService(ILogger logger, INuGetService nuGetService, IGitHubService gitHubService, ISettings settings, IShell shell)
    {
        _logger = logger;
        _nuGetService = nuGetService;
        _gitHubService = gitHubService;
        _settings = settings;
        _shell = shell;

        // Subscribe to settings changes to invalidate cache when prerelease preference changes
        _settings.SettingsChanged += OnSettingsChanged;

        // Subscribe to refresh requests to re-check for updates
        _shell.RefreshRequested += OnRefreshRequested;
    }

    public VersionCheckCompletedEventArgs? LastKnownVersions { get; private set; }

    public void WhenVersionCheckUpdates(Action<VersionCheckCompletedEventArgs> callback)
    {
        if (LastKnownVersions != null)
            callback(LastKnownVersions);
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

            // Build arguments - include --prerelease if user wants prerelease updates
            var args = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings()).IncludePrereleaseUpdates
                ? $"new install {EnvironmentMetadata.TemplatePackageId} --prerelease"
                : $"new install {EnvironmentMetadata.TemplatePackageId}";

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = args,
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

            _logger.Info($"Template package installed successfully (prerelease: {_settings.GetValue(SettingsKeys.HubSettings, new HubSettings()).IncludePrereleaseUpdates})");
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

    void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        // Invalidate cached version check results when HubSettings change
        // (specifically when IncludePrereleaseUpdates changes)
        if (e.Key == SettingsKeys.HubSettings)
        {
            _logger.Info("Settings changed, invalidating cached version check results");
            LastKnownVersions = null;
            _ = CheckForUpdatesAsync(); // Fire and forget
        }
    }

    void OnRefreshRequested(object? sender, EventArgs e)
    {
        _logger.Info("Refresh requested - re-checking for updates");
        LastKnownVersions = null;
        _ = CheckForUpdatesAsync(); // Fire and forget
    }

    async Task<IReadOnlyList<PackageVersionInfo>> CheckNuGetPackagesAsync(CancellationToken cancellationToken)
    {
        _logger.Info("Checking NuGet packages for updates");

        var packages = EnvironmentMetadata.AllPackageIds;

        var includePrerelease = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings()).IncludePrereleaseUpdates;
        var results = new List<PackageVersionInfo>();

        foreach (var packageId in packages)
        {
            var version = await _nuGetService.GetLatestVersionAsync(packageId, includePrerelease, cancellationToken);
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

        var includePrerelease = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings()).IncludePrereleaseUpdates;
        var latestVersion = await _nuGetService.GetLatestVersionAsync(EnvironmentMetadata.TemplatePackageId, includePrerelease, cancellationToken);
        if (latestVersion == null)
        {
            _logger.Warning("Could not determine latest template version");
            return null;
        }

        // Get installed template version
        var installedVersion = await GetInstalledTemplateVersionAsync();
        if (installedVersion == null)
        {
            _logger.Info("Template package not installed");
            return new TemplateVersionInfo
            {
                LatestVersion = latestVersion
            };
        }

        _logger.Info($"Template package installed version: {installedVersion}, latest: {latestVersion}");

        // Use semantic version comparison
        if (NuGetVersion.TryParse(installedVersion, out var installedVer) && NuGetVersion.TryParse(latestVersion, out var latestVer) && latestVer > installedVer)
        {
            _logger.Info($"Template update available: {installedVersion} -> {latestVersion}");
            return new TemplateVersionInfo
            {
                LatestVersion = latestVersion
            };
        }

        _logger.Info("Template package is up to date");
        return null;
    }

    async Task<string?> GetInstalledTemplateVersionAsync()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "new uninstall",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Force English output to avoid localization issues
            startInfo.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en-US";

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.Error("Failed to start dotnet process");
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Parse output to find our template package and its version
            // Format: "   Mal.Mdk2.ScriptTemplates\n      Version: 2.2.50"
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(EnvironmentMetadata.TemplatePackageId, StringComparison.OrdinalIgnoreCase))
                {
                    // Version is on the next line or two after package name
                    for (var j = i + 1; j < Math.Min(i + 5, lines.Length); j++)
                    {
                        var line = lines[j].Trim();
                        if (line.StartsWith("Version:", StringComparison.OrdinalIgnoreCase))
                        {
                            var version = line.Substring("Version:".Length).Trim();
                            _logger.Debug($"Found installed template version: {version}");
                            return version;
                        }
                    }
                }
            }

            _logger.Debug("Template package not found in installed templates");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get installed template version: {ex.Message}");
            return null;
        }
    }

    async Task<HubVersionInfo?> CheckHubVersionAsync(CancellationToken cancellationToken)
    {
        _logger.Info("Checking Hub version for updates");

        var includePrerelease = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings()).IncludePrereleaseUpdates;
        var version = await _gitHubService.GetLatestReleaseAsync(EnvironmentMetadata.GitHubOwner, EnvironmentMetadata.GitHubRepo, includePrerelease, cancellationToken);
        if (version != null)
        {
            // Get current version and compare
            var currentVersionString = GetCurrentHubVersion();
            var latestVersionString = version.Value.Version;

            // Strip "hub-v" prefix if present
            if (latestVersionString.StartsWith("hub-v", StringComparison.OrdinalIgnoreCase))
                latestVersionString = latestVersionString.Substring(6);

            _logger.Info($"Current version: {currentVersionString}, Latest version: {latestVersionString}");

            // Parse semantic versions for proper comparison
            try
            {
                var currentVersion = NuGetVersion.Parse(currentVersionString);
                var latestVersion = NuGetVersion.Parse(latestVersionString);

                // Only return update info if the latest version is newer than current
                if (latestVersion > currentVersion)
                {
                    _logger.Info($"Update available: {latestVersionString} is newer than {currentVersionString}");
                    return new HubVersionInfo
                    {
                        LatestVersion = latestVersionString,
                        IsPrerelease = version.Value.IsPrerelease,
                        DownloadUrl = $"{EnvironmentMetadata.GitHubRepoUrl}/releases/latest"
                    };
                }
                _logger.Info($"Already running the latest version (current: {currentVersion}, latest: {latestVersion})");
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to parse versions for comparison: {ex.Message}. Current: {currentVersionString}, Latest: {latestVersionString}");
            }
        }

        return null;
    }

    string GetCurrentHubVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (version != null)
        {
            // Strip git metadata (everything after +)
            var plusIndex = version.IndexOf('+');
            if (plusIndex >= 0)
                version = version.Substring(0, plusIndex);
        }

        return version ?? "unknown";
    }
}