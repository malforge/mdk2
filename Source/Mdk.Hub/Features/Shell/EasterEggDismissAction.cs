using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Projects.Actions;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Shell;

[ViewModelFor<EasterEggDismissActionView>]
public partial class EasterEggDismissAction : ActionItem
{
    readonly IShell _shell;
    readonly ICommonDialogs _dialogs;

    public EasterEggDismissAction(IShell shell, ICommonDialogs dialogs)
    {
        _shell = shell;
        _dialogs = dialogs;
    }

    public override string? Category => "EasterEgg";

    public override bool ShouldShow(ProjectListItem? selectedProject, bool canMakeScript, bool canMakeMod)
    {
        return _shell.IsEasterEggActive;
    }

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
