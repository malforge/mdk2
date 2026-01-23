using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Projects.Actions;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Shell;

[ViewModelFor<EasterEggDismissActionView>]
public partial class EasterEggDismissAction(IShell shell, ICommonDialogs dialogs) : ActionItem
{
    readonly ICommonDialogs _dialogs = dialogs;
    readonly IShell _shell = shell;

    public override string Category => "EasterEgg";

    public override bool ShouldShow(ProjectListItem? selectedProject, bool canMakeScript, bool canMakeMod) => _shell.IsEasterEggActive;

    [RelayCommand]
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

    [RelayCommand]
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