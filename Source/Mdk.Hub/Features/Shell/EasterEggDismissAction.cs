using System.Threading.Tasks;
using System.Windows.Input;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Projects.Actions;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Shell;

[ViewModelFor<EasterEggDismissActionView>]
public class EasterEggDismissAction : ActionItem
{
    readonly ICommonDialogs _dialogs;
    readonly IShell _shell;
    readonly AsyncRelayCommand _disableForTodayCommand;
    readonly AsyncRelayCommand _disableForeverCommand;

    public EasterEggDismissAction(IShell shell, ICommonDialogs dialogs)
    {
        _dialogs = dialogs;
        _shell = shell;
        _disableForTodayCommand = new AsyncRelayCommand(DisableForToday);
        _disableForeverCommand = new AsyncRelayCommand(DisableForever);
    }

    public ICommand DisableForTodayCommand => _disableForTodayCommand;
    public ICommand DisableForeverCommand => _disableForeverCommand;

    public override string Category => "EasterEgg";

    public override bool ShouldShow(ProjectModel? selectedProject, bool canMakeScript, bool canMakeMod) => _shell.IsEasterEggActive;

    async Task DisableForToday()
    {
        var result = await _dialogs.ShowAsync(new ConfirmationMessage
        {
            Title = "Disable Easter Egg",
            Message = "Hide the easter egg for today? It will return next year on this date.",
            OkText = "Hide for Today",
            CancelText = "Cancel"
        });

        if (result)
            _shell.DisableEasterEggForToday();
    }

    async Task DisableForever()
    {
        var result = await _dialogs.ShowAsync(new ConfirmationMessage
        {
            Title = "Disable Easter Egg Forever",
            Message = "Hide the easter egg permanently? This cannot be undone.",
            OkText = "Hide Forever",
            CancelText = "Cancel"
        });

        if (result)
            _shell.DisableEasterEggForever();
    }
}