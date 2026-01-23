using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Settings;

[Dependency<GlobalSettingsViewModel>]
[ViewModelFor<GlobalSettingsView>]
public partial class GlobalSettingsViewModel : OverlayModel
{
    readonly GlobalSettings _globalSettings;
    string? _customAutoScriptOutputPath;
    string? _customAutoModOutputPath;
    string? _customAutoBinaryPath;

    public GlobalSettingsViewModel(GlobalSettings globalSettings)
    {
        _globalSettings = globalSettings;
        _customAutoScriptOutputPath = _globalSettings.CustomAutoScriptOutputPath;
        _customAutoModOutputPath = _globalSettings.CustomAutoModOutputPath;
        _customAutoBinaryPath = _globalSettings.CustomAutoBinaryPath;
    }

    public string? CustomAutoScriptOutputPath
    {
        get => _customAutoScriptOutputPath;
        set => SetProperty(ref _customAutoScriptOutputPath, value);
    }

    public string? CustomAutoModOutputPath
    {
        get => _customAutoModOutputPath;
        set => SetProperty(ref _customAutoModOutputPath, value);
    }

    public string? CustomAutoBinaryPath
    {
        get => _customAutoBinaryPath;
        set => SetProperty(ref _customAutoBinaryPath, value);
    }

    [RelayCommand]
    void Save()
    {
        _globalSettings.CustomAutoScriptOutputPath = _customAutoScriptOutputPath;
        _globalSettings.CustomAutoModOutputPath = _customAutoModOutputPath;
        _globalSettings.CustomAutoBinaryPath = _customAutoBinaryPath;
        Dismiss();
    }

    [RelayCommand]
    void Cancel()
    {
        Dismiss();
    }
}
