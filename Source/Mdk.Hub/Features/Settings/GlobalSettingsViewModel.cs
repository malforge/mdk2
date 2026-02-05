using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Mal.DependencyInjection;

using Mdk.Hub.Features.Shell;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Updates;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Settings;

[Instance]
[ViewModelFor<GlobalSettingsView>]
public class GlobalSettingsViewModel : OverlayModel
{
    readonly AsyncRelayCommand _cancelCommand;
    readonly AsyncRelayCommand _checkPrerequisitesCommand;
    readonly ISettings _settings;
    readonly RelayCommand _saveCommand;
    readonly IShell _shell;
    readonly IUpdateManager _updateManager;
    readonly HubSettings _hubSettings;
    string _customAutoBinaryPath = "";
    string _customAutoModOutputPath = "";
    string _customAutoScriptOutputPath = "";
    bool _includePrereleaseUpdates;
    bool _openedForLinuxValidation;

    public GlobalSettingsViewModel(ISettings settings, IUpdateManager updateManager, IShell shell, ILogger logger)
    {
        _settings = settings;
        _updateManager = updateManager;
        _shell = shell;
        _hubSettings = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings());
        
        // Load initial values
        LoadFromSettings();
        
        // Subscribe to external settings changes
        _settings.SettingsChanged += OnSettingsChanged;
        
        _saveCommand = new RelayCommand(Save);
        _cancelCommand = new AsyncRelayCommand(CancelAsync, logger: logger);
        _checkPrerequisitesCommand = new AsyncRelayCommand(CheckPrerequisitesAsync);
    }
    
    void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        // Reload if HubSettings changed externally (not by us during Save)
        if (e.Key == SettingsKeys.HubSettings)
        {
            LoadFromSettings();
        }
    }
    
    void LoadFromSettings()
    {
        var settings = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings());
        
        // On Linux, "auto" is not valid - convert to empty string for editing
        var scriptPath = settings.CustomAutoScriptOutputPath;
        var modPath = settings.CustomAutoModOutputPath;
        var binPath = settings.CustomAutoBinaryPath;
        
        CustomAutoScriptOutputPath = (App.IsLinux && scriptPath == "auto") ? "" : scriptPath;
        CustomAutoModOutputPath = (App.IsLinux && modPath == "auto") ? "" : modPath;
        CustomAutoBinaryPath = (App.IsLinux && binPath == "auto") ? "" : binPath;
        IncludePrereleaseUpdates = settings.IncludePrereleaseUpdates;
    }

    public ICommand SaveCommand => _saveCommand;
    public ICommand CancelCommand => _cancelCommand;
    public ICommand CheckPrerequisitesCommand => _checkPrerequisitesCommand;
    
    public event EventHandler? FocusFirstInvalidField;
    
    public void MarkAsOpenedForLinuxValidation()
    {
        _openedForLinuxValidation = true;
        
        // Delay to let the UI render, then focus the first invalid field
        Task.Delay(600).ContinueWith(_ =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                FocusFirstInvalidField?.Invoke(this, EventArgs.Empty);
            });
        });
    }

    public string CustomAutoScriptOutputPath
    {
        get => _customAutoScriptOutputPath;
        set
        {
            if (SetProperty(ref _customAutoScriptOutputPath, value))
            {
                OnPropertyChanged(nameof(ScriptPathValidationError));
                OnPropertyChanged(nameof(HasScriptPathError));
            }
        }
    }

    public string CustomAutoModOutputPath
    {
        get => _customAutoModOutputPath;
        set
        {
            if (SetProperty(ref _customAutoModOutputPath, value))
            {
                OnPropertyChanged(nameof(ModPathValidationError));
                OnPropertyChanged(nameof(HasModPathError));
            }
        }
    }

    public string CustomAutoBinaryPath
    {
        get => _customAutoBinaryPath;
        set
        {
            if (SetProperty(ref _customAutoBinaryPath, value))
            {
                OnPropertyChanged(nameof(BinaryPathValidationError));
                OnPropertyChanged(nameof(HasBinaryPathError));
            }
        }
    }

    public bool IncludePrereleaseUpdates
    {
        get => _includePrereleaseUpdates;
        set => SetProperty(ref _includePrereleaseUpdates, value);
    }
    
    public bool IsLinux => App.IsLinux;
    
    public bool HasScriptPathError => !string.IsNullOrWhiteSpace(ScriptPathValidationError);
    public bool HasModPathError => !string.IsNullOrWhiteSpace(ModPathValidationError);
    public bool HasBinaryPathError => !string.IsNullOrWhiteSpace(BinaryPathValidationError);
    
    public string? ScriptPathValidationError =>
        App.IsLinux && (string.IsNullOrWhiteSpace(_customAutoScriptOutputPath) || _customAutoScriptOutputPath == "auto")
            ? "⚠ Please set a valid path" 
            : null;
    
    public string? ModPathValidationError => 
        App.IsLinux && (string.IsNullOrWhiteSpace(_customAutoModOutputPath) || _customAutoModOutputPath == "auto")
            ? "⚠ Please set a valid path" 
            : null;
    
    public string? BinaryPathValidationError => 
        App.IsLinux && (string.IsNullOrWhiteSpace(_customAutoBinaryPath) || _customAutoBinaryPath == "auto")
            ? "⚠ Please set a valid path" 
            : App.IsLinux && !string.IsNullOrWhiteSpace(_customAutoBinaryPath) && !IsValidBinaryPath(_customAutoBinaryPath)
            ? "⚠ Path does not contain Space Engineers binaries"
            : null;
    
    static bool IsValidBinaryPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "auto")
            return false;
            
        if (!Directory.Exists(path))
            return false;
        
        // Check for key SE binaries that MDK projects reference
        var requiredBinaries = new[]
        {
            "Sandbox.Common.dll",
            "Sandbox.Game.dll",
            "SpaceEngineers.Game.dll",
            "VRage.dll"
        };
        
        // At least one must exist
        foreach (var binary in requiredBinaries)
        {
            if (File.Exists(Path.Combine(path, binary)))
                return true;
        }
        
        return false;
    }

    void Save()
    {
        // On Linux, paths are required
        if (App.IsLinux)
        {
            var errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(_customAutoBinaryPath) || _customAutoBinaryPath == "auto")
                errors.Add("Space Engineers binary path is required on Linux");
                
            if (string.IsNullOrWhiteSpace(_customAutoScriptOutputPath) || _customAutoScriptOutputPath == "auto")
                errors.Add("Script output path is required on Linux");
                
            if (string.IsNullOrWhiteSpace(_customAutoModOutputPath) || _customAutoModOutputPath == "auto")
                errors.Add("Mod output path is required on Linux");
            
            if (errors.Count > 0)
            {
                _shell.ShowToast("Please set all required paths for Linux");
                return;
            }
        }
        
        var updatedSettings = _hubSettings with
        {
            CustomAutoScriptOutputPath = _customAutoScriptOutputPath,
            CustomAutoModOutputPath = _customAutoModOutputPath,
            CustomAutoBinaryPath = _customAutoBinaryPath,
            IncludePrereleaseUpdates = _includePrereleaseUpdates
        };
        _settings.SetValue(SettingsKeys.HubSettings, updatedSettings);
        Dismiss();
    }

    async Task CancelAsync()
    {
        // On Linux, if dialog was auto-opened for validation and SAVED paths are still invalid, confirm shutdown
        if (App.IsLinux && _openedForLinuxValidation)
        {
            // Check the ACTUAL saved settings, not the ViewModel fields
            var savedSettings = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings());
            var savedBinPath = savedSettings.CustomAutoBinaryPath;
            var savedScriptPath = savedSettings.CustomAutoScriptOutputPath;
            var savedModPath = savedSettings.CustomAutoModOutputPath;
            
            var hasErrors = 
                savedBinPath == "auto" || savedScriptPath == "auto" || savedModPath == "auto";
            
            if (hasErrors)
            {
                var result = await _shell.ShowOverlayAsync(new ConfirmationMessage
                {
                    Title = "Invalid Configuration",
                    Message = "The Hub requires valid paths to function. Do you want to shut down the Hub?",
                    OkText = "Shut Down",
                    CancelText = "Go Back"
                });

                // If user chose to go back, do not dismiss
                if (!result) return;
            }
        }
        
        Dismiss();
    }

    async Task CheckPrerequisitesAsync()
    {
        var busyOverlay = new BusyOverlayViewModel("Checking prerequisites...");
        _shell.AddOverlay(busyOverlay);

        try
        {
            var messages = new List<string>();

            // Check .NET SDK
            busyOverlay.Message = "Checking .NET SDK...";
            var (sdkInstalled, sdkVersion) = await _updateManager.CheckDotNetSdkAsync();
            if (sdkInstalled)
                messages.Add($"✓ .NET SDK {sdkVersion} is installed");
            else
                messages.Add("✗ .NET SDK 9.0 or later is not installed");

            // Check template package
            busyOverlay.Message = "Checking template package...";
            var templateInstalled = await _updateManager.IsTemplateInstalledAsync();
            if (templateInstalled)
                messages.Add("✓ MDK² template package is installed");
            else
                messages.Add("✗ MDK² template package is not installed");

            busyOverlay.Dismiss();

            // Only show dialog if there are problems - otherwise just toast
            var hasProblems = messages.Any(m => m.StartsWith("✗"));
            if (hasProblems)
            {
                await _shell.ShowOverlayAsync(new InformationMessage
                {
                    Title = "Prerequisites Check",
                    Message = string.Join("\n", messages)
                });
            }
            else
            {
                _shell.ShowToast("All prerequisites installed ✓");
            }
        }
        catch (Exception ex)
        {
            busyOverlay.Dismiss();
            await _shell.ShowOverlayAsync(new InformationMessage
            {
                Title = "Check Failed",
                Message = $"An error occurred while checking prerequisites:\n\n{ex.Message}"
            });
        }
    }
}

