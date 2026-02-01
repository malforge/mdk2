using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Mal.DependencyInjection;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Features.Updates;
using Mdk.Hub.Framework;
using Velopack;
using Velopack.Sources;

namespace Mdk.Hub.Features.Projects.Actions.Items;

/// <summary>
///     Global action for checking and installing updates to Hub and templates.
/// </summary>
[Singleton]
[ViewModelFor<UpdatesActionView>]
public class UpdatesAction : ActionItem
{
    readonly IShell _shell;
    readonly IUpdateCheckService _updateCheckService;
    double _downloadProgress;
    HubVersionInfo? _hubVersionInfo;
    bool _isDownloading;
    bool _isHubUpdateAvailable;
    bool _isReadyToInstall;
    bool _isTemplateUpdateAvailable;
    UpdateInfo? _pendingUpdate;

    public UpdatesAction(IShell shell, IUpdateCheckService updateCheckService)
    {
        _shell = shell;
        _updateCheckService = updateCheckService;
        
        UpdateTemplatesCommand = new RelayCommand(UpdateTemplates);
        UpdateHubCommand = new RelayCommand(UpdateHub);
        InstallHubUpdateCommand = new RelayCommand(InstallHubUpdate);

        // Subscribe to update check results
        _updateCheckService.WhenVersionCheckCompleted(OnVersionCheckCompleted);

        // Subscribe to refresh requests
        _shell.RefreshRequested += OnRefreshRequested;
    }

    void OnRefreshRequested(object? sender, EventArgs e)
    {
        // Force a fresh update check
        _ = _updateCheckService.CheckForUpdatesAsync();
    }

    public override string? Category => null; // No category - appears at top
    public override bool IsGlobal => true; // This is a global action, not project-specific

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

    public bool IsDownloading
    {
        get => _isDownloading;
        set
        {
            if (SetProperty(ref _isDownloading, value))
                UpdateStatusMessage();
        }
    }

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

    public double DownloadProgress
    {
        get => _downloadProgress;
        set => SetProperty(ref _downloadProgress, value);
    }

    public HubVersionInfo? HubVersionInfo
    {
        get => _hubVersionInfo;
        set
        {
            if (SetProperty(ref _hubVersionInfo, value))
            {
                UpdateStatusMessage();
                OnPropertyChanged(nameof(UpdateHubButtonText));
            }
        }
    }

    string _statusMessage = "Checking for updates...";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string UpdateHubButtonText => HubVersionInfo?.IsPrerelease == true ? "Update Hub (prerelease)" : "Update Hub";

    public ICommand UpdateTemplatesCommand { get; }
    public ICommand UpdateHubCommand { get; }
    public ICommand InstallHubUpdateCommand { get; }

    void UpdateStatusMessage()
    {
        if (IsDownloading)
            StatusMessage = $"Downloading Hub update... {DownloadProgress:P0}";
        else if (IsReadyToInstall)
            StatusMessage = "Hub update ready to install";
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

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"new install {EnvironmentMetadata.TemplatePackageId} --force",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                StatusMessage = "Failed to start template update";
                return;
            }

            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                _shell.ShowToast("Templates updated successfully");
                UpdateStatusMessage();
            }
            else
            {
                var error = await process.StandardError.ReadToEndAsync();
                StatusMessage = $"Update failed: {error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Update error: {ex.Message}";
        }
    }

    async void UpdateHub()
    {
        try
        {
            IsHubUpdateAvailable = false;
            IsDownloading = true;
            DownloadProgress = 0;

            var mgr = new UpdateManager(new GithubSource(EnvironmentMetadata.GitHubRepoUrl, null, false));
            var newVersion = await mgr.CheckForUpdatesAsync();
            
            if (newVersion == null)
            {
                IsDownloading = false;
                StatusMessage = "No update available";
                return;
            }

            await mgr.DownloadUpdatesAsync(newVersion, progress =>
            {
                DownloadProgress = progress / 100.0;
            });

            _pendingUpdate = newVersion;
            IsDownloading = false;
            IsReadyToInstall = true;
        }
        catch (Exception ex)
        {
            IsDownloading = false;
            StatusMessage = $"Update failed: {ex.Message}";
        }
    }

    async void InstallHubUpdate()
    {
        if (_pendingUpdate == null)
        {
            StatusMessage = "No update ready to install";
            return;
        }

        try
        {
            var confirmed = await _shell.ShowAsync(new ConfirmationMessage
            {
                Title = "Install Hub Update",
                Message = "Installing the update will restart MDK Hub.\n\nContinue?",
                OkText = "Install Now",
                CancelText = "Cancel"
            });

            if (!confirmed)
                return;

            var mgr = new UpdateManager(new GithubSource(EnvironmentMetadata.GitHubRepoUrl, null, false));
            mgr.ApplyUpdatesAndRestart(_pendingUpdate);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to install: {ex.Message}";
        }
    }
}
