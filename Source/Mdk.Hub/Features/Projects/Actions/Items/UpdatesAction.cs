using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Features.Updates;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Actions.Items;

/// <summary>
///     Global action for checking and installing updates to Hub and templates.
/// </summary>
/// <remarks>
///     Updating the Hub is a single decision: press the button, and the download installs itself the next time the Hub
///     is closed. There is no second step, and the running session is never interrupted.
/// </remarks>
[Singleton]
[ViewModelFor<UpdatesActionView>]
public class UpdatesAction : ActionItem
{
    readonly ILogger _logger;
    readonly IShell _shell;
    readonly IUpdateManager _updateManager;
    double _downloadProgress;
    string? _hubUpdateError;
    HubVersionInfo? _hubVersionInfo;
    bool _isDownloading;
    bool _isHubUpdateAvailable;
    bool _isPendingInstall;
    bool _isTemplateUpdateAvailable;

    string _statusMessage = "Checking for updates...";

    string _title = "Updates Available";

    string _updateHubButtonText = "Update Hub";

    /// <summary>
    ///     Initializes a new instance of the <see cref="UpdatesAction" /> class.
    /// </summary>
    /// <param name="shell">The shell interface for UI interactions.</param>
    /// <param name="updateManager">The manager for handling updates.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public UpdatesAction(IShell shell, IUpdateManager updateManager, ILogger logger)
    {
        _shell = shell;
        _updateManager = updateManager;
        _logger = logger;

        UpdateTemplatesCommand = new RelayCommand(UpdateTemplates);
        UpdateHubCommand = new AsyncRelayCommand(UpdateHubAsync);

        // A download from an earlier session that never got to install still only needs the Hub to close
        IsPendingInstall = _updateManager.IsHubUpdatePendingInstall;

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
    ///     Gets or sets whether a Hub update is available to download.
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
    ///     Gets or sets whether a Hub update has been downloaded and will install when the Hub is closed.
    /// </summary>
    public bool IsPendingInstall
    {
        get => _isPendingInstall;
        set
        {
            if (SetProperty(ref _isPendingInstall, value))
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
        set
        {
            // The status message quotes the percentage, so the two must never drift apart
            if (SetProperty(ref _downloadProgress, value))
                UpdateStatusMessage();
        }
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
    ///     Gets the card heading. Nothing is "available" once the update has been downloaded, so the heading follows the
    ///     state rather than standing still.
    /// </summary>
    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    /// <summary>
    ///     Gets or sets the status message displayed to the user.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

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
    ///     Gets the command to update the Hub. This is the only step the user takes: the download installs itself the next
    ///     time the Hub is closed.
    /// </summary>
    public ICommand UpdateHubCommand { get; }

    void OnRefreshRequested(object? sender, EventArgs e) =>
        // Force a fresh update check
        _ = _updateManager.CheckForUpdatesAsync();

    /// <summary>
    ///     Determines whether this action should be shown in the UI.
    /// </summary>
    public override bool ShouldShow() => IsTemplateUpdateAvailable || IsHubUpdateAvailable || IsDownloading || IsPendingInstall;

    void OnVersionCheckCompleted(VersionCheckCompletedEventArgs args)
    {
        // Check if template update is available
        IsTemplateUpdateAvailable = args.TemplatePackage != null;

        if (args.HubVersion != null)
            HubVersionInfo = args.HubVersion;

        // Nothing to offer if it is already downloaded, or if this build cannot update itself at all
        IsHubUpdateAvailable = args.HubVersion != null && !IsPendingInstall && !IsDownloading && _updateManager.IsHubUpdateSupported;

        // Update status message
        UpdateStatusMessage();
    }

    void UpdateStatusMessage()
    {
        var hubVersion = HubVersionInfo?.LatestVersion ?? "unknown";
        var hubSuffix = HubVersionInfo?.IsPrerelease == true ? " (prerelease)" : "";

        Title = IsDownloading
            ? "Downloading Update"
            : IsPendingInstall && !IsTemplateUpdateAvailable
                ? "Update Ready"
                : "Updates Available";

        if (IsDownloading)
            StatusMessage = $"Downloading Hub {hubVersion}{hubSuffix}... {DownloadProgress:P0}";
        else if (_hubUpdateError != null)
            StatusMessage = $"Hub update failed: {_hubUpdateError}";
        else if (IsPendingInstall && IsTemplateUpdateAvailable)
            StatusMessage = $"Hub {hubVersion}{hubSuffix} will be installed the next time you close the Hub, and a templates update is available";
        else if (IsPendingInstall)
            StatusMessage = $"Hub {hubVersion}{hubSuffix} is downloaded - it will be installed the next time you close the Hub";
        else if (IsTemplateUpdateAvailable && IsHubUpdateAvailable)
            StatusMessage = $"Templates and Hub {hubVersion}{hubSuffix} updates available";
        else if (IsTemplateUpdateAvailable)
            StatusMessage = "Templates update available";
        else if (IsHubUpdateAvailable)
            StatusMessage = $"Hub {hubVersion}{hubSuffix} update available";
        else
            StatusMessage = "All up to date";
    }

    async Task UpdateHubAsync()
    {
        try
        {
            _hubUpdateError = null;
            IsHubUpdateAvailable = false;
            IsDownloading = true;
            DownloadProgress = 0;

            var progress = new Progress<UpdateProgress>(p =>
            {
                if (p.PercentComplete.HasValue)
                    DownloadProgress = p.PercentComplete.Value / 100.0;
            });

            var result = await _updateManager.DownloadHubUpdateAsync(progress);

            IsDownloading = false;

            if (result.Success)
                IsPendingInstall = true;
            else
            {
                // Put the button back so the user can try again
                _hubUpdateError = result.ErrorMessage ?? "Unknown error";
                IsHubUpdateAvailable = true;
                _logger.Error($"Hub update download failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            IsDownloading = false;
            _hubUpdateError = ex.Message;
            IsHubUpdateAvailable = true;
            _logger.Error("Hub update download failed", ex);
        }
        finally
        {
            UpdateStatusMessage();
        }
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
}
