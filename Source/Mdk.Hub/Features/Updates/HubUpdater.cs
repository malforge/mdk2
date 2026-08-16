using System;
using System.Threading;
using System.Threading.Tasks;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Settings;
using Velopack.Sources;

namespace Mdk.Hub.Features.Updates;

/// <summary>
///     Internal utility class for Hub application updates using Velopack.
///     Cross-platform: Works on both Windows and Linux.
/// </summary>
/// <remarks>
///     This is the only place in the Hub that talks to Velopack. The user asks for the download; once it is on disk
///     Velopack keeps it there, and it installs itself the next time the Hub is closed. Nothing interrupts the session
///     that asked for it, and the download is never repeated.
/// </remarks>
internal class HubUpdater
{
    readonly ILogger _logger;
    readonly ISettings _settings;
    bool _applyScheduled;

    public HubUpdater(ISettings settings, ILogger logger)
    {
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    ///     Whether this Hub can update itself. False for development and portable builds, which Velopack does not manage.
    /// </summary>
    public bool IsSupported
    {
        get
        {
            try
            {
                return CreateManager().IsInstalled;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Hub updates are unavailable: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    ///     Whether an update has already been downloaded and is waiting to be installed when the Hub closes.
    /// </summary>
    public bool IsUpdatePendingInstall
    {
        get
        {
            try
            {
                var manager = CreateManager();
                return manager.IsInstalled && manager.UpdatePendingRestart is not null;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Could not determine pending update state: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    ///     Downloads the latest Hub update and stages it for installation on the next restart.
    /// </summary>
    public async Task<UpdateResult> DownloadAsync(IProgress<UpdateProgress>? progress, CancellationToken cancellationToken)
    {
        try
        {
            var manager = CreateManager();
            if (!manager.IsInstalled)
            {
                _logger.Info("Hub updates are unavailable for this installation");
                return Failure("This Hub installation cannot update itself");
            }

            if (manager.UpdatePendingRestart is not null)
            {
                _logger.Info("A Hub update is already downloaded and waiting to be installed");
                return new UpdateResult { Success = true };
            }

            progress?.Report(new UpdateProgress { Message = "Checking for updates...", PercentComplete = 0 });
            var newVersion = await manager.CheckForUpdatesAsync();

            if (newVersion == null)
            {
                _logger.Info("No Hub update available");
                return Failure("No update available");
            }

            _logger.Info($"Downloading Hub update: {newVersion.TargetFullRelease.Version}");
            progress?.Report(new UpdateProgress { Message = "Downloading update...", PercentComplete = 0 });

            await manager.DownloadUpdatesAsync(newVersion,
                percent => progress?.Report(new UpdateProgress
                {
                    Message = $"Downloading update... {percent}%",
                    PercentComplete = percent
                }),
                cancellationToken);

            _logger.Info($"Hub update {newVersion.TargetFullRelease.Version} downloaded, and will install when the Hub is closed");
            return new UpdateResult
            {
                Success = true,
                UpdatedItems = [newVersion.TargetFullRelease.Version.ToString()]
            };
        }
        catch (OperationCanceledException)
        {
            _logger.Info("Hub update download cancelled");
            return Failure("Update cancelled");
        }
        catch (Exception ex)
        {
            _logger.Error("Hub update download failed", ex);
            return new UpdateResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Exception = ex
            };
        }
    }

    /// <summary>
    ///     Installs a downloaded update once the Hub has exited, without relaunching it. Called while shutting down, so
    ///     the update the user asked for lands without ever interrupting them.
    /// </summary>
    /// <returns>True if an update was scheduled.</returns>
    public bool ApplyOnExit()
    {
        if (_applyScheduled)
            return true;

        try
        {
            var manager = CreateManager();
            if (!manager.IsInstalled)
                return false;

            var pending = manager.UpdatePendingRestart;
            if (pending is null)
                return false;

            manager.WaitExitThenApplyUpdates(pending, true, false);
            _applyScheduled = true;
            _logger.Info($"Hub update {pending.Version} will be installed once the Hub has closed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to schedule the Hub update", ex);
            return false;
        }
    }

    Velopack.UpdateManager CreateManager()
    {
        var includePrerelease = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings()).IncludePrereleaseUpdates;
        return new Velopack.UpdateManager(new GithubSource(EnvironmentMetadata.GitHubRepoUrl, null, includePrerelease));
    }

    static UpdateResult Failure(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };
}
