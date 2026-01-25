using System.Windows.Input;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Settings;

[Dependency<GlobalSettingsViewModel>]
[ViewModelFor<GlobalSettingsView>]
public class GlobalSettingsViewModel : OverlayModel
{
    readonly GlobalSettings _globalSettings;
    string _customAutoScriptOutputPath;
    string _customAutoModOutputPath;
    string _customAutoBinaryPath;
    readonly RelayCommand _saveCommand;
    readonly RelayCommand _cancelCommand;

    public GlobalSettingsViewModel(GlobalSettings globalSettings)
    {
        _globalSettings = globalSettings;
        _customAutoScriptOutputPath = _globalSettings.CustomAutoScriptOutputPath;
        _customAutoModOutputPath = _globalSettings.CustomAutoModOutputPath;
        _customAutoBinaryPath = _globalSettings.CustomAutoBinaryPath;
        _saveCommand = new RelayCommand(Save);
        _cancelCommand = new RelayCommand(Cancel);
    }

    public ICommand SaveCommand => _saveCommand;
    public ICommand CancelCommand => _cancelCommand;

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
}