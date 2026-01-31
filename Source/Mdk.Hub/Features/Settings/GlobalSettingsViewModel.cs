using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Mal.DependencyInjection;

using Mdk.Hub.Features.Shell;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Updates;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Settings;

[Singleton<GlobalSettingsViewModel>]
[ViewModelFor<GlobalSettingsView>]
public class GlobalSettingsViewModel : OverlayModel
{
    readonly RelayCommand _cancelCommand;
    readonly RelayCommand _checkPrerequisitesCommand;
    readonly GlobalSettings _globalSettings;
    readonly RelayCommand _saveCommand;
    readonly IShell _shell;
    readonly IUpdateCheckService _updateCheckService;
    string _customAutoBinaryPath;
    string _customAutoModOutputPath;
    string _customAutoScriptOutputPath;
    bool _includePrereleaseUpdates;

    public GlobalSettingsViewModel(GlobalSettings globalSettings, IUpdateCheckService updateCheckService, IShell shell)
    {
        _globalSettings = globalSettings;
        _updateCheckService = updateCheckService;
        _shell = shell;
        _customAutoScriptOutputPath = _globalSettings.CustomAutoScriptOutputPath;
        _customAutoModOutputPath = _globalSettings.CustomAutoModOutputPath;
        _customAutoBinaryPath = _globalSettings.CustomAutoBinaryPath;
        _includePrereleaseUpdates = _globalSettings.IncludePrereleaseUpdates;
        _saveCommand = new RelayCommand(Save);
        _cancelCommand = new RelayCommand(Cancel);
        _checkPrerequisitesCommand = new RelayCommand(async () => await CheckPrerequisitesAsync());
    }

    public ICommand SaveCommand => _saveCommand;
    public ICommand CancelCommand => _cancelCommand;
    public ICommand CheckPrerequisitesCommand => _checkPrerequisitesCommand;

    public string CustomAutoScriptOutputPath
    {
        get => _customAutoScriptOutputPath;
        set => SetProperty(ref _customAutoScriptOutputPath, value);
    }

    public string CustomAutoModOutputPath
    {
        get => _customAutoModOutputPath;
        set => SetProperty(ref _customAutoModOutputPath, value);
    }

    public string CustomAutoBinaryPath
    {
        get => _customAutoBinaryPath;
        set => SetProperty(ref _customAutoBinaryPath, value);
    }

    public bool IncludePrereleaseUpdates
    {
        get => _includePrereleaseUpdates;
        set => SetProperty(ref _includePrereleaseUpdates, value);
    }

    void Save()
    {
        _globalSettings.CustomAutoScriptOutputPath = _customAutoScriptOutputPath;
        _globalSettings.CustomAutoModOutputPath = _customAutoModOutputPath;
        _globalSettings.CustomAutoBinaryPath = _customAutoBinaryPath;
        _globalSettings.IncludePrereleaseUpdates = _includePrereleaseUpdates;
        Dismiss();
    }

    void Cancel() => Dismiss();

    async Task CheckPrerequisitesAsync()
    {
        var busyOverlay = new BusyOverlayViewModel("Checking prerequisites...");
        _shell.AddOverlay(busyOverlay);

        try
        {
            var messages = new List<string>();

            // Check .NET SDK
            busyOverlay.Message = "Checking .NET SDK...";
            var (sdkInstalled, sdkVersion) = await _updateCheckService.CheckDotNetSdkAsync();
            if (sdkInstalled)
                messages.Add($"✓ .NET SDK {sdkVersion} is installed");
            else
                messages.Add("✗ .NET SDK 9.0 or later is not installed");

            // Check template package
            busyOverlay.Message = "Checking template package...";
            var templateInstalled = await _updateCheckService.IsTemplateInstalledAsync();
            if (templateInstalled)
                messages.Add("✓ MDK² template package is installed");
            else
                messages.Add("✗ MDK² template package is not installed");

            busyOverlay.Dismiss();

            await _shell.ShowAsync(new InformationMessage
            {
                Title = "Prerequisites Check",
                Message = string.Join("\n", messages)
            });
        }
        catch (Exception ex)
        {
            busyOverlay.Dismiss();
            await _shell.ShowAsync(new InformationMessage
            {
                Title = "Check Failed",
                Message = $"An error occurred while checking prerequisites:\n\n{ex.Message}"
            });
        }
    }
}
