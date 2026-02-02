using System;
using System.Threading;
using System.Threading.Tasks;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Settings;
using Velopack;
using Velopack.Sources;

namespace Mdk.Hub.Features.Updates;

/// <summary>
///     Internal utility class for Hub application updates using Velopack.
///     Cross-platform: Works on both Windows and Linux.
/// </summary>
internal class HubUpdater
{
    readonly ILogger _logger;
    readonly ISettings _settings;

    public HubUpdater(ISettings settings, ILogger logger)
    {
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    ///     Updates the Hub application to the latest version.
    /// </summary>
    public async Task<UpdateResult> UpdateAsync(HubVersionInfo versionInfo, IProgress<UpdateProgress>? progress, CancellationToken cancellationToken)
    {
        try
        {
            progress?.Report(new UpdateProgress { Message = "Initializing Hub update...", PercentComplete = 0 });

            var includePrerelease = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings()).IncludePrereleaseUpdates;
            
            _logger.Info($"Creating GithubSource with RepoUrl={EnvironmentMetadata.GitHubRepoUrl}, Channel=null, Prerelease={includePrerelease}");
            var source = new GithubSource(EnvironmentMetadata.GitHubRepoUrl, null, includePrerelease);
            var mgr = new Velopack.UpdateManager(source);

            progress?.Report(new UpdateProgress { Message = "Checking for updates...", PercentComplete = 10 });
            
            _logger.Info("Calling Velopack CheckForUpdatesAsync...");
            var newVersion = await mgr.CheckForUpdatesAsync();
            _logger.Info($"Velopack returned: {(newVersion == null ? "null (no updates)" : $"version {newVersion.TargetFullRelease.Version}")}");

            if (newVersion == null)
            {
                _logger.Info("No Hub update available from Velopack");
                return new UpdateResult
                {
                    Success = false,
                    ErrorMessage = "No update available"
                };
            }

            progress?.Report(new UpdateProgress { Message = "Downloading update...", PercentComplete = 20 });

            await mgr.DownloadUpdatesAsync(newVersion, p =>
            {
                var percent = 20 + (p * 0.7); // 20% to 90%
                progress?.Report(new UpdateProgress
                {
                    Message = $"Downloading update... {p}%",
                    PercentComplete = percent
                });
            });

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.Info("Hub update cancelled");
                return new UpdateResult
                {
                    Success = false,
                    ErrorMessage = "Update cancelled by user"
                };
            }

            progress?.Report(new UpdateProgress { Message = "Applying update and restarting...", PercentComplete = 95 });

            _logger.Info($"Applying Hub update to version {newVersion.TargetFullRelease.Version}");
            
            // This will restart the application - does not return
            mgr.ApplyUpdatesAndRestart(newVersion);

            // If we reach here, something went wrong
            return new UpdateResult
            {
                Success = false,
                ErrorMessage = "Failed to apply update and restart"
            };
        }
        catch (OperationCanceledException)
        {
            _logger.Info("Hub update cancelled");
            return new UpdateResult
            {
                Success = false,
                ErrorMessage = "Update cancelled"
            };
        }
        catch (Exception ex)
        {
            _logger.Error("Hub update failed", ex);
            return new UpdateResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Exception = ex
            };
        }
    }
}
