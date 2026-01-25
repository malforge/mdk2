using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Diagnostics;

namespace Mdk.Hub.Features.Updates;

[Dependency<IUpdateCheckService>]
public class UpdateCheckService(ILogger logger, INuGetService nuGetService, IGitHubService gitHubService) : IUpdateCheckService
{
    readonly ILogger _logger = logger;
    readonly INuGetService _nuGetService = nuGetService;
    readonly IGitHubService _gitHubService = gitHubService;
    int _isChecking = 0; // 0 = not checking, 1 = checking
    
    public event EventHandler<VersionCheckCompletedEventArgs>? VersionCheckCompleted;
    
    public VersionCheckCompletedEventArgs? LastKnownVersions { get; private set; }

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
            VersionCheckCompleted?.Invoke(this, results);
            
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

        var version = await _gitHubService.GetLatestReleaseAsync("malforge", "mdk2", cancellationToken);
        if (version != null)
        {
            return new HubVersionInfo 
            { 
                LatestVersion = version,
                DownloadUrl = "https://github.com/malforge/mdk2/releases/latest"
            };
        }

        return null;
    }
}
