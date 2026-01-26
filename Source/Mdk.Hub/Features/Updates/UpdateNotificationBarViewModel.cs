using System;
using System.Windows.Input;
using Mal.DependencyInjection;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Framework;

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
    string? _downloadedMsiPath;

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
        if (HubVersionInfo == null) return;
        
        // Check platform
        if (OperatingSystem.IsWindows())
        {
            await DownloadHubUpdateAsync();
        }
        else if (OperatingSystem.IsLinux())
        {
            OpenBrowserToReleases();
        }
    }
    
    async System.Threading.Tasks.Task DownloadHubUpdateAsync()
    {
        if (HubVersionInfo == null) return;
        
        try
        {
            // Set downloading state
            IsHubUpdateAvailable = false;
            IsDownloading = true;
            DownloadProgress = 0;
            UpdateMessage();
            
            // Create temp path for MSI
            var tempPath = System.IO.Path.GetTempPath();
            var fileName = $"MdkHub-{HubVersionInfo.LatestVersion}.msi";
            _downloadedMsiPath = System.IO.Path.Combine(tempPath, fileName);
            
            // Download with progress
            using var client = new System.Net.Http.HttpClient();
            using var response = await client.GetAsync(HubVersionInfo.DownloadUrl, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            
            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new System.IO.FileStream(_downloadedMsiPath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None, 8192, true);
            
            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;
            
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;
                
                if (totalBytes > 0)
                {
                    DownloadProgress = (double)totalRead / totalBytes;
                    UpdateMessage();
                }
            }
            
            // Download complete
            IsDownloading = false;
            IsReadyToInstall = true;
            UpdateMessage();
        }
        catch (Exception ex)
        {
            IsDownloading = false;
            Message = $"Download failed: {ex.Message}";
        }
    }
    
    void OpenBrowserToReleases()
    {
        try
        {
            var url = "https://github.com/malware-dev/mdk2/releases";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            
            // Dismiss the notification after opening browser
            IsVisible = false;
        }
        catch (Exception ex)
        {
            Message = $"Failed to open browser: {ex.Message}";
        }
    }

    async void InstallHubUpdate()
    {
        if (string.IsNullOrEmpty(_downloadedMsiPath) || !System.IO.File.Exists(_downloadedMsiPath))
        {
            Message = "Update file not found";
            return;
        }
        
        try
        {
            // Show confirmation dialog
            var confirmed = await _commonDialogs.ShowAsync(new ConfirmationMessage
            {
                Title = "Install Hub Update",
                Message = "Installing the update will close MDK Hub. The installer will launch and you can reopen the Hub when installation completes.\n\nContinue?",
                OkText = "Install Now",
                CancelText = "Cancel"
            });
            
            if (!confirmed)
                return;
            
            // Launch MSI installer
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "msiexec.exe",
                Arguments = $"/i \"{_downloadedMsiPath}\"",
                UseShellExecute = false
            });
            
            // Exit the application
            System.Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Message = $"Failed to launch installer: {ex.Message}";
        }
    }
}
