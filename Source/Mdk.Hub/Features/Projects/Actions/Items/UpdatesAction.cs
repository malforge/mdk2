using System;
using System.Windows.Input;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Settings;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Features.Updates;
using Mdk.Hub.Framework;
using Velopack.Sources;

namespace Mdk.Hub.Features.Projects.Actions.Items;

/// <summary>
///     Global action for checking and installing updates to Hub and templates.
/// </summary>
[Singleton]
[ViewModelFor<UpdatesActionView>]
public class UpdatesAction : ActionItem
{
    readonly ILogger _logger;
    readonly ISettings _settings;
    readonly IShell _shell;
    readonly IUpdateManager _updateManager;
    double _downloadProgress;
    HubVersionInfo? _hubVersionInfo;
    bool _isDownloading;
    bool _isHubUpdateAvailable;
    bool _isReadyToInstall;
    bool _isTemplateUpdateAvailable;

    string _statusMessage = "Checking for updates...";

    /// <summary>
    ///     Initializes a new instance of the <see cref="UpdatesAction"/> class.
    /// </summary>
    /// <param name="settings">The settings manager for user preferences.</param>
    /// <param name="shell">The shell interface for UI interactions.</param>
    /// <param name="updateManager">The manager for handling updates.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public UpdatesAction(ISettings settings, IShell shell, IUpdateManager updateManager, ILogger logger)
    {
        _settings = settings;
        _shell = shell;
        _updateManager = updateManager;
        _logger = logger;

        UpdateTemplatesCommand = new RelayCommand(UpdateTemplates);
        UpdateHubCommand = new RelayCommand(UpdateHub);
        InstallHubUpdateCommand = new RelayCommand(InstallHubUpdate);

        // Subscribe to update check results
        updateManager.WhenVersionCheckUpdates(OnVersionCheckCompleted);

        // Subscribe to refresh requests
        _shell.RefreshRequested += OnRefreshRequested;
    }

    /// <summary>
    ///     Gets the action category (null = no category, appears at top).
    /// </summary>
    public override string? Category => null; // No category - appears at top
    
    /// <summary>
    ///     Gets whether this is a global action (not project-specific).
    /// </summary>
    public override bool IsGlobal => true; // This is a global action, not project-specific

    /// <summary>
    ///     Gets or sets whether a template update is available.
    /// </summary>
    public bool IsTemplateUpdateAvailable
    {
        get => _isTemplateUpdateAvailable;
        set
        {
            if (SetProperty(ref _isTemplateUpdateAvailable, value))
            {
                UpdateStatusMessage();
                RaiseShouldShowChanged();
            }
        }
    }

    /// <summary>
    ///     Gets or sets whether a Hub update is available.
    /// </summary>
    public bool IsHubUpdateAvailable
    {
        get => _isHubUpdateAvailable;
        set
        {
            if (SetProperty(ref _isHubUpdateAvailable, value))
            {
                UpdateStatusMessage();
                RaiseShouldShowChanged();
            }
        }
    }

    /// <summary>
    ///     Gets or sets whether a Hub update is ready to install.
    /// </summary>
    public bool IsReadyToInstall
    {
        get => _isReadyToInstall;
        set
        {
            if (SetProperty(ref _isReadyToInstall, value))
            {
                UpdateStatusMessage();
                RaiseShouldShowChanged();
            }
        }
    }

    /// <summary>
    ///     Gets or sets whether a Hub update is currently downloading.
    /// </summary>
    public bool IsDownloading
    {
        get => _isDownloading;
        set
        {
            if (SetProperty(ref _isDownloading, value))
            {
                UpdateStatusMessage();
                RaiseShouldShowChanged();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the download progress (0.0 to 1.0).
    /// </summary>
    public double DownloadProgress
    {
        get => _downloadProgress;
        set => SetProperty(ref _downloadProgress, value);
    }

    /// <summary>
    ///     Gets or sets the Hub version information for available updates.
    /// </summary>
    public HubVersionInfo? HubVersionInfo
    {
        get => _hubVersionInfo;
        set
        {
            if (SetProperty(ref _hubVersionInfo, value))
            {
                UpdateStatusMessage();
                UpdateHubButtonText = value?.IsPrerelease == true ? "Update Hub (prerelease)" : "Update Hub";
            }
        }
    }

    /// <summary>
    ///     Gets or sets the status message displayed to the user.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    string _updateHubButtonText = "Update Hub";

    /// <summary>
    ///     Gets the text for the Update Hub button.
    /// </summary>
    public string UpdateHubButtonText
    {
        get => _updateHubButtonText;
        private set => SetProperty(ref _updateHubButtonText, value);
    }

    /// <summary>
    ///     Gets the command to update templates.
    /// </summary>
    public ICommand UpdateTemplatesCommand { get; }
    
    /// <summary>
    ///     Gets the command to download a Hub update.
    /// </summary>
    public ICommand UpdateHubCommand { get; }
    
    /// <summary>
    ///     Gets the command to install a downloaded Hub update.
    /// </summary>
    public ICommand InstallHubUpdateCommand { get; }

    void OnRefreshRequested(object? sender, EventArgs e) =>
        // Force a fresh update check
        _ = _updateManager.CheckForUpdatesAsync();

    /// <summary>
    ///     Determines whether this action should be shown in the UI.
    /// </summary>
    public override bool ShouldShow() => IsTemplateUpdateAvailable || IsHubUpdateAvailable || IsDownloading || IsReadyToInstall;

    void OnVersionCheckCompleted(VersionCheckCompletedEventArgs args)
    {
        // Check if template update is available
        IsTemplateUpdateAvailable = args.TemplatePackage != null;

        // Check if Hub update is available
        if (args.HubVersion != null)
        {
            IsHubUpdateAvailable = true;
            HubVersionInfo = args.HubVersion;
        }

        // Update status message
        UpdateStatusMessage();
    }

    void UpdateStatusMessage()
    {
        if (IsDownloading)
            StatusMessage = $"Downloading Hub update... {DownloadProgress:P0}";
        else if (IsReadyToInstall)
            StatusMessage = "Hub update ready to install - click Install Now";
        else if (IsTemplateUpdateAvailable && IsHubUpdateAvailable)
        {
            var hubVersion = HubVersionInfo?.LatestVersion ?? "unknown";
            var hubSuffix = HubVersionInfo?.IsPrerelease == true ? " (prerelease)" : "";
            StatusMessage = $"Templates and Hub {hubVersion}{hubSuffix} updates available";
        }
        else if (IsTemplateUpdateAvailable)
            StatusMessage = "Templates update available";
        else if (IsHubUpdateAvailable)
        {
            var version = HubVersionInfo?.LatestVersion ?? "unknown";
            var suffix = HubVersionInfo?.IsPrerelease == true ? " (prerelease)" : "";
            StatusMessage = $"Hub {version}{suffix} update available";
        }
        else
            StatusMessage = "All up to date";
    }

    async void UpdateTemplates()
    {
        try
        {
            StatusMessage = "Updating templates...";
            IsTemplateUpdateAvailable = false;

            var progress = new Progress<UpdateProgress>(p => StatusMessage = p.Message);
            var result = await _updateManager.UpdateTemplatesAsync(progress);

            if (result.Success)
            {
                _shell.ShowToast("Templates updated successfully");
                UpdateStatusMessage();
            }
            else
            {
                StatusMessage = $"Update failed: {result.ErrorMessage}";
                _shell.ShowToast("Template update failed");
                if (result.Exception != null)
                    _logger.Error($"Template update failed: {result.ErrorMessage}", result.Exception);
                else
                    _logger.Error($"Template update failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Update error: {ex.Message}";
            _shell.ShowToast("Template update failed");
            _logger.Error("Template update failed", ex);
        }
    }

    async void UpdateHub()
    {
        try
        {
            _logger.Info("Starting Hub update download");
            IsDownloading = true;
            DownloadProgress = 0;

            var progress = new Progress<UpdateProgress>(p =>
            {
                StatusMessage = p.Message;
                if (p.PercentComplete.HasValue)
                    DownloadProgress = p.PercentComplete.Value / 100.0;
            });

            // ONLY download - do not install automatically (user agency)
            // UpdateHubAsync will download but not restart since we're not calling ApplyUpdatesAndRestart
            // We need to refactor HubUpdater to support download-only mode
            
            // For now, use direct Velopack for two-step flow
            var includePrerelease = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings()).IncludePrereleaseUpdates;
            _logger.Info($"Creating Velopack UpdateManager (includePrerelease={includePrerelease})");
            var mgr = new Velopack.UpdateManager(new GithubSource(EnvironmentMetadata.GitHubRepoUrl, null, includePrerelease));
            
            _logger.Info("Checking for Hub updates via Velopack");
            var newVersion = await mgr.CheckForUpdatesAsync();

            if (newVersion == null)
            {
                _logger.Info("Velopack reports no Hub update available");
                IsDownloading = false;
                StatusMessage = "No update available";
                return;
            }

            _logger.Info($"Downloading Hub update: {newVersion.TargetFullRelease.Version}");
            _logger.Info($"Calling DownloadUpdatesAsync with progress callback...");
            await mgr.DownloadUpdatesAsync(newVersion, p => { 
                DownloadProgress = p / 100.0;
                _logger.Debug($"Download progress: {p}%");
            });
            _logger.Info($"DownloadUpdatesAsync completed successfully");

            IsDownloading = false;
            IsHubUpdateAvailable = false; // Clear the update flag now that download is complete
            IsReadyToInstall = true;
            _logger.Info($"Hub update downloaded and ready to install: {newVersion.TargetFullRelease.Version}");
        }
        catch (Exception ex)
        {
            IsDownloading = false;
            StatusMessage = $"Update failed: {ex.Message}";
            _logger.Error("Hub update download failed", ex);
        }
    }

    async void InstallHubUpdate()
    {
        try
        {
            // Explicit user confirmation required
            var confirmed = await _shell.ShowOverlayAsync(new ConfirmationMessage
            {
                Title = "Install Hub Update",
                Message = "Installing the update will restart MDK Hub.\n\nContinue?",
                OkText = "Install Now",
                CancelText = "Cancel"
            });

            if (!confirmed)
            {
                _logger.Info("User cancelled Hub update installation");
                return;
            }

            _logger.Info("User confirmed Hub update installation, proceeding");
            StatusMessage = "Installing update and restarting...";

            // Now use UpdateManager to actually install
            var progress = new Progress<UpdateProgress>(p => StatusMessage = p.Message);
            var result = await _updateManager.UpdateHubAsync(progress);

            if (!result.Success)
            {
                StatusMessage = $"Installation failed: {result.ErrorMessage}";
                if (result.Exception != null)
                    _logger.Error($"Hub update installation failed: {result.ErrorMessage}", result.Exception);
                else
                    _logger.Error($"Hub update installation failed: {result.ErrorMessage}");
                _shell.ShowToast("Hub update installation failed");
            }
            // If successful, app will restart and we won't reach here
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to install: {ex.Message}";
            _logger.Error("Failed to install Hub update", ex);
            _shell.ShowToast("Hub update installation failed");
        }
    }
}
