using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Projects;
using Mdk.Hub.Features.Settings;
using Mdk.Hub.Utility;
using NuGet.Versioning;

namespace Mdk.Hub.Features.Updates;

/// <summary>
///     Internal utility class for NuGet package updates with backup/rollback support.
///     Cross-platform: Uses platform-agnostic file APIs and CanonicalPath.
/// </summary>
internal class PackageUpdater
{
    readonly ILogger _logger;
    readonly INuGetService _nugetService;
    readonly ISettings _settings;

    public PackageUpdater(INuGetService nugetService, ISettings settings, ILogger logger)
    {
        _nugetService = nugetService;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    ///     Updates NuGet packages for a project with automatic backup creation.
    /// </summary>
    public async Task<UpdateResult> UpdateProjectAsync(CanonicalPath projectPath, IProgress<UpdateProgress>? progress, CancellationToken cancellationToken)
    {
        if (projectPath.IsEmpty() || !File.Exists(projectPath.Value))
        {
            _logger.Warning($"Cannot update packages: project file not found at {projectPath}");
            return new UpdateResult
            {
                Success = false,
                ErrorMessage = "Project file not found"
            };
        }

        try
        {
            progress?.Report(new UpdateProgress { Message = "Checking for package updates...", PercentComplete = 0 });

            // Get packages that need updating
            var packagesToUpdate = await GetPackageUpdatesAsync(projectPath, cancellationToken);

            if (packagesToUpdate.Count == 0)
            {
                _logger.Info("No packages to update");
                return new UpdateResult
                {
                    Success = true,
                    UpdatedItems = Array.Empty<string>()
                };
            }

            progress?.Report(new UpdateProgress { Message = $"Backing up project file...", PercentComplete = 20 });

            // Create backup before modification
            if (!CreateBackup(projectPath))
            {
                return new UpdateResult
                {
                    Success = false,
                    ErrorMessage = "Failed to create backup file"
                };
            }

            progress?.Report(new UpdateProgress { Message = $"Updating {packagesToUpdate.Count} package(s)...", PercentComplete = 40 });

            _logger.Info($"Updating {packagesToUpdate.Count} package(s) in {projectPath.Value}");

            // Load and modify the .csproj file
            var doc = XDocument.Load(projectPath.Value!);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
            var modified = false;
            var updatedItems = new List<string>();

            foreach (var update in packagesToUpdate)
            {
                // Find the PackageReference element
                var packageRef = doc.Descendants(ns + "PackageReference")
                    .FirstOrDefault(e => e.Attribute("Include")?.Value == update.PackageId);

                if (packageRef != null)
                {
                    var versionAttr = packageRef.Attribute("Version");
                    if (versionAttr != null)
                    {
                        _logger.Info($"Updating {update.PackageId}: {versionAttr.Value} -> {update.LatestVersion}");
                        versionAttr.Value = update.LatestVersion;
                        modified = true;
                        updatedItems.Add($"{update.PackageId} ({update.CurrentVersion} → {update.LatestVersion})");
                    }
                    else
                        _logger.Warning($"PackageReference for {update.PackageId} found but has no Version attribute");
                }
                else
                    _logger.Warning($"PackageReference for {update.PackageId} not found in project file");
            }

            if (!modified)
            {
                _logger.Warning("No packages were updated in the project file");
                DeleteBackup(projectPath); // Clean up backup since nothing changed
                return new UpdateResult
                {
                    Success = false,
                    ErrorMessage = "No packages were updated"
                };
            }

            progress?.Report(new UpdateProgress { Message = "Saving project file...", PercentComplete = 60 });

            // Save the modified project file
            doc.Save(projectPath.Value!);
            _logger.Info($"Saved updated project file: {projectPath.Value}");

            progress?.Report(new UpdateProgress { Message = "Running dotnet restore...", PercentComplete = 70 });

            // Run dotnet restore to apply the changes
            _logger.Info($"Running dotnet restore for {projectPath.Value}");
            var restoreSuccess = await RunDotnetRestoreAsync(projectPath, cancellationToken);

            if (restoreSuccess)
            {
                progress?.Report(new UpdateProgress { Message = "Update complete!", PercentComplete = 100 });
                _logger.Info($"Package update complete for {projectPath.Value}");

                // Keep backup file for potential rollback
                return new UpdateResult
                {
                    Success = true,
                    UpdatedItems = updatedItems
                };
            }

            progress?.Report(new UpdateProgress { Message = "Restore failed, rolling back...", PercentComplete = 80 });
            _logger.Error($"dotnet restore failed for {projectPath.Value}, rolling back changes");

            // Restore failed - rollback changes
            if (RestoreFromBackup(projectPath))
            {
                return new UpdateResult
                {
                    Success = false,
                    ErrorMessage = "Package update failed during restore. Changes have been rolled back."
                };
            }

            return new UpdateResult
            {
                Success = false,
                ErrorMessage = "Package update failed during restore. Rollback also failed - manual intervention required."
            };
        }
        catch (OperationCanceledException)
        {
            _logger.Info($"Package update cancelled for {projectPath}");
            
            // Attempt to rollback on cancellation
            if (File.Exists(GetBackupPath(projectPath)))
            {
                RestoreFromBackup(projectPath);
            }

            return new UpdateResult
            {
                Success = false,
                ErrorMessage = "Update cancelled"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to update packages for {projectPath}", ex);
            
            // Attempt to rollback on error
            if (File.Exists(GetBackupPath(projectPath)))
            {
                RestoreFromBackup(projectPath);
            }

            return new UpdateResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Exception = ex
            };
        }
    }

    /// <summary>
    ///     Checks if a backup file exists for the project.
    /// </summary>
    public Task<bool> CanRollbackAsync(CanonicalPath projectPath)
    {
        var backupPath = GetBackupPath(projectPath);
        return Task.FromResult(File.Exists(backupPath));
    }

    /// <summary>
    ///     Rolls back NuGet package updates by restoring from backup.
    /// </summary>
    public async Task<UpdateResult> RollbackProjectAsync(CanonicalPath projectPath)
    {
        if (projectPath.IsEmpty() || !File.Exists(projectPath.Value))
        {
            return new UpdateResult
            {
                Success = false,
                ErrorMessage = "Project file not found"
            };
        }

        var backupPath = GetBackupPath(projectPath);
        if (!File.Exists(backupPath))
        {
            return new UpdateResult
            {
                Success = false,
                ErrorMessage = "No backup file found for rollback"
            };
        }

        try
        {
            _logger.Info($"Rolling back package updates for {projectPath.Value}");

            // Restore from backup
            if (!RestoreFromBackup(projectPath))
            {
                return new UpdateResult
                {
                    Success = false,
                    ErrorMessage = "Failed to restore from backup"
                };
            }

            // Run dotnet restore to apply the rollback
            var restoreSuccess = await RunDotnetRestoreAsync(projectPath, CancellationToken.None);

            if (restoreSuccess)
            {
                _logger.Info($"Package rollback complete for {projectPath.Value}");
                DeleteBackup(projectPath); // Clean up backup after successful rollback
                
                return new UpdateResult
                {
                    Success = true,
                    UpdatedItems = new[] { "Packages rolled back to previous versions" }
                };
            }

            return new UpdateResult
            {
                Success = false,
                ErrorMessage = "Rollback completed but dotnet restore failed"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to rollback packages for {projectPath}", ex);
            return new UpdateResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Exception = ex
            };
        }
    }

    async Task<List<PackageUpdateInfo>> GetPackageUpdatesAsync(CanonicalPath projectPath, CancellationToken cancellationToken)
    {
        var updates = new List<PackageUpdateInfo>();

        var currentVersions = GetMdkPackageVersions(projectPath);
        var includePrereleases = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings()).IncludePrereleaseUpdates;

        var checkTasks = currentVersions.Select(async kvp =>
        {
            var (packageId, currentVersion) = kvp;
            var latestVersion = await _nugetService.GetLatestVersionAsync(packageId, includePrereleases, cancellationToken);

            if (latestVersion == null)
                return null;

            var current = NuGetVersion.Parse(currentVersion);
            var latest = NuGetVersion.Parse(latestVersion);

            if (latest > current)
            {
                return new PackageUpdateInfo
                {
                    PackageId = packageId,
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion
                };
            }

            return null;
        });

        var results = await Task.WhenAll(checkTasks);
        updates.AddRange(results.Where(r => r != null)!);

        return updates;
    }

    Dictionary<string, string> GetMdkPackageVersions(CanonicalPath projectPath)
    {
        var versions = new Dictionary<string, string>();

        try
        {
            var doc = XDocument.Load(projectPath.Value!);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

            var packageRefs = doc.Descendants(ns + "PackageReference")
                .Where(e =>
                {
                    var include = e.Attribute("Include")?.Value;
                    return include != null && include.StartsWith("Malware.MDKUtilities", StringComparison.OrdinalIgnoreCase);
                });

            foreach (var packageRef in packageRefs)
            {
                var packageId = packageRef.Attribute("Include")?.Value;
                var version = packageRef.Attribute("Version")?.Value;

                if (packageId != null && version != null)
                    versions[packageId] = version;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to read package versions from {projectPath}", ex);
        }

        return versions;
    }

    async Task<bool> RunDotnetRestoreAsync(CanonicalPath projectPath, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"restore \"{projectPath.Value}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return false;

            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to run dotnet restore for {projectPath}", ex);
            return false;
        }
    }

    string GetBackupPath(CanonicalPath projectPath)
    {
        // Use platform-agnostic path operations
        return projectPath.Value + ".backup";
    }

    bool CreateBackup(CanonicalPath projectPath)
    {
        try
        {
            var backupPath = GetBackupPath(projectPath);
            File.Copy(projectPath.Value!, backupPath, overwrite: true);
            _logger.Info($"Created backup: {backupPath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to create backup for {projectPath}", ex);
            return false;
        }
    }

    bool RestoreFromBackup(CanonicalPath projectPath)
    {
        try
        {
            var backupPath = GetBackupPath(projectPath);
            if (!File.Exists(backupPath))
            {
                _logger.Warning($"Backup file not found: {backupPath}");
                return false;
            }

            File.Copy(backupPath, projectPath.Value!, overwrite: true);
            _logger.Info($"Restored from backup: {backupPath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to restore from backup for {projectPath}", ex);
            return false;
        }
    }

    void DeleteBackup(CanonicalPath projectPath)
    {
        try
        {
            var backupPath = GetBackupPath(projectPath);
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
                _logger.Info($"Deleted backup: {backupPath}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to delete backup for {projectPath}", ex);
        }
    }
}

