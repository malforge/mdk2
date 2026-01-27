using System.Windows.Input;
using Mal.DependencyInjection;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Features.Updates;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Settings;

[Dependency<GlobalSettingsViewModel>]
[ViewModelFor<GlobalSettingsView>]
public class GlobalSettingsViewModel : OverlayModel
{
    readonly GlobalSettings _globalSettings;
    readonly IUpdateCheckService _updateCheckService;
    readonly ICommonDialogs _commonDialogs;
    readonly IShell _shell;
    string _customAutoScriptOutputPath;
    string _customAutoModOutputPath;
    string _customAutoBinaryPath;
    readonly RelayCommand _saveCommand;
    readonly RelayCommand _cancelCommand;
    readonly RelayCommand _checkPrerequisitesCommand;

    public GlobalSettingsViewModel(GlobalSettings globalSettings, IUpdateCheckService updateCheckService, ICommonDialogs commonDialogs, IShell shell)
    {
        _globalSettings = globalSettings;
        _updateCheckService = updateCheckService;
        _commonDialogs = commonDialogs;
        _shell = shell;
        _customAutoScriptOutputPath = _globalSettings.CustomAutoScriptOutputPath;
        _customAutoModOutputPath = _globalSettings.CustomAutoModOutputPath;
        _customAutoBinaryPath = _globalSettings.CustomAutoBinaryPath;
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

    void Save()
    {
        _globalSettings.CustomAutoScriptOutputPath = _customAutoScriptOutputPath;
        _globalSettings.CustomAutoModOutputPath = _customAutoModOutputPath;
        _globalSettings.CustomAutoBinaryPath = _customAutoBinaryPath;
        Dismiss();
    }

    void Cancel()
    {
        Dismiss();
    }

    async System.Threading.Tasks.Task CheckPrerequisitesAsync()
    {
        var busyOverlay = new BusyOverlayViewModel("Checking prerequisites...");
        _shell.AddOverlay(busyOverlay);

        try
        {
            var messages = new System.Collections.Generic.List<string>();

            // Check .NET SDK
            busyOverlay.Message = "Checking .NET SDK...";
            var (sdkInstalled, sdkVersion) = await _updateCheckService.CheckDotNetSdkAsync();
            if (sdkInstalled)
            {
                messages.Add($"✓ .NET SDK {sdkVersion} is installed");
            }
            else
            {
                messages.Add("✗ .NET SDK 9.0 or later is not installed");
            }

            // Check template package
            busyOverlay.Message = "Checking template package...";
            var templateInstalled = await _updateCheckService.IsTemplateInstalledAsync();
            if (templateInstalled)
            {
                messages.Add("✓ MDK² template package is installed");
            }
            else
            {
                messages.Add("✗ MDK² template package is not installed");
            }

            busyOverlay.Dismiss();

            await _commonDialogs.ShowAsync(new InformationMessage
            {
                Title = "Prerequisites Check",
                Message = string.Join("\n", messages)
            });
        }
        catch (System.Exception ex)
        {
            busyOverlay.Dismiss();
            await _commonDialogs.ShowAsync(new InformationMessage
            {
                Title = "Check Failed",
                Message = $"An error occurred while checking prerequisites:\n\n{ex.Message}"
            });
        }
    }
}