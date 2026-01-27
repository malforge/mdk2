using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Mal.DependencyInjection;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Framework;
using Velopack;
using Velopack.Sources;

namespace Mdk.Hub.Features.Updates;

[Dependency]
[ViewModelFor<UpdateNotificationBarView>]
public class UpdateNotificationBarViewModel : ViewModel
{
    readonly ICommonDialogs _commonDialogs;
    string _message = "";
    bool _isVisible;
    bool _isTemplateUpdateAvailable;
    bool _isHubUpdateAvailable;
    bool _isDownloading;
    bool _isReadyToInstall;
    double _downloadProgress;
    HubVersionInfo? _hubVersionInfo;
    UpdateInfo? _pendingUpdate;

    public UpdateNotificationBarViewModel(ICommonDialogs commonDialogs)
    {
        _commonDialogs = commonDialogs;
        DismissCommand = new RelayCommand(Dismiss);
        UpdateTemplatesCommand = new RelayCommand(UpdateTemplates);
        UpdateHubCommand = new RelayCommand(UpdateHub);
        InstallHubUpdateCommand = new RelayCommand(InstallHubUpdate);
    }

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public bool IsTemplateUpdateAvailable
    {
        get => _isTemplateUpdateAvailable;
        set => SetProperty(ref _isTemplateUpdateAvailable, value);
    }

    public bool IsHubUpdateAvailable
    {
        get => _isHubUpdateAvailable;
        set => SetProperty(ref _isHubUpdateAvailable, value);
    }

    public bool IsDownloading
    {
        get => _isDownloading;
        set => SetProperty(ref _isDownloading, value);
    }

    public bool IsReadyToInstall
    {
        get => _isReadyToInstall;
        set => SetProperty(ref _isReadyToInstall, value);
    }

    public double DownloadProgress
    {
        get => _downloadProgress;
        set => SetProperty(ref _downloadProgress, value);
    }
    
    public HubVersionInfo? HubVersionInfo
    {
        get => _hubVersionInfo;
        set => SetProperty(ref _hubVersionInfo, value);
    }

    public ICommand DismissCommand { get; }
    public ICommand UpdateTemplatesCommand { get; }
    public ICommand UpdateHubCommand { get; }
    public ICommand InstallHubUpdateCommand { get; }

    public void UpdateMessage()
    {
        if (IsDownloading)
        {
            Message = $"Downloading Hub update... {DownloadProgress:P0}";
        }
        else if (IsReadyToInstall)
        {
            Message = "Hub update ready to install";
        }
        else if (IsTemplateUpdateAvailable && IsHubUpdateAvailable)
        {
            Message = "Script templates and Hub updates available";
        }
        else if (IsTemplateUpdateAvailable)
        {
            Message = "Script templates update available";
        }
        else if (IsHubUpdateAvailable)
        {
            Message = "Hub update available";
        }
    }

    void Dismiss()
    {
        IsVisible = false;
    }

    async void UpdateTemplates()
    {
        try
        {
            // Show progress
            Message = "Updating script templates...";
            IsTemplateUpdateAvailable = false;
            OnPropertyChanged(nameof(IsTemplateUpdateAvailable));
            
            // Run dotnet new install
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "new install Mal.Mdk2.ScriptTemplates --force",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null)
            {
                Message = "Failed to start template update";
                return;
            }
            
            await process.WaitForExitAsync();
            
            if (process.ExitCode == 0)
            {
                Message = "Script templates updated successfully";
                await System.Threading.Tasks.Task.Delay(3000);
                IsVisible = false;
            }
            else
            {
                var error = await process.StandardError.ReadToEndAsync();
                Message = $"Template update failed: {error}";
            }
        }
        catch (Exception ex)
        {
            Message = $"Template update error: {ex.Message}";
        }
    }

    async void UpdateHub()
    {
        try
        {
            // Set downloading state
            IsHubUpdateAvailable = false;
            IsDownloading = true;
            DownloadProgress = 0;
            UpdateMessage();
            
            // Use Velopack to download update (works on both Windows and Linux!)
            var mgr = new UpdateManager(new GithubSource("https://github.com/malware-dev/mdk2", null, false));
            
            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion == null)
            {
                IsDownloading = false;
                Message = "No update available";
                return;
            }
            
            // Download with progress
            await mgr.DownloadUpdatesAsync(newVersion, progress =>
            {
                DownloadProgress = progress / 100.0;
                UpdateMessage();
            });
            
            // Download complete - save for install
            _pendingUpdate = newVersion;
            IsDownloading = false;
            IsReadyToInstall = true;
            UpdateMessage();
        }
        catch (Exception ex)
        {
            IsDownloading = false;
            Message = $"Update failed: {ex.Message}";
        }
    }

    async void InstallHubUpdate()
    {
        if (_pendingUpdate == null)
        {
            Message = "No update ready to install";
            return;
        }
        
        try
        {
            // Show confirmation dialog
            var confirmed = await _commonDialogs.ShowAsync(new ConfirmationMessage
            {
                Title = "Install Hub Update",
                Message = "Installing the update will restart MDK Hub.\n\nContinue?",
                OkText = "Install Now",
                CancelText = "Cancel"
            });
            
            if (!confirmed)
                return;
            
            // Use Velopack to apply update and restart (works on both Windows and Linux!)
            var mgr = new UpdateManager(new GithubSource("https://github.com/malware-dev/mdk2", null, false));
            mgr.ApplyUpdatesAndRestart(_pendingUpdate);
        }
        catch (Exception ex)
        {
            Message = $"Failed to install update: {ex.Message}";
        }
    }
}
