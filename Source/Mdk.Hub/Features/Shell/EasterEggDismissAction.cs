using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Mal.DependencyInjection;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Projects.Actions;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Shell;

[Singleton]
[ViewModelFor<EasterEggDismissActionView>]
public class EasterEggDismissAction : ActionItem
{
    readonly AsyncRelayCommand _disableForeverCommand;
    readonly AsyncRelayCommand _disableForTodayCommand;
    readonly IEasterEggService _easterEggService;
    readonly IShell _shell;

    public EasterEggDismissAction(IShell shell, IEasterEggService easterEggService)
    {
        _shell = shell;
        _easterEggService = easterEggService;
        _disableForTodayCommand = new AsyncRelayCommand(DisableForToday);
        _disableForeverCommand = new AsyncRelayCommand(DisableForever);
    }

    public ICommand DisableForTodayCommand => _disableForTodayCommand;
    public ICommand DisableForeverCommand => _disableForeverCommand;

    public override string Category => "EasterEgg";

    public override bool ShouldShow() => _easterEggService.IsActive;

    async Task DisableForToday()
    {
        var result = await _shell.ShowOverlayAsync(new ConfirmationMessage
        {
            Title = "Disable Easter Egg",
            Message = "Hide the easter egg for today? It will return next year on this date.",
            OkText = "Hide for Today",
            CancelText = "Cancel"
        });

        if (result)
            _easterEggService.DisableFor(TimeSpan.FromDays(1));
    }

    async Task DisableForever()
    {
        var result = await _shell.ShowOverlayAsync(new ConfirmationMessage
        {
            Title = "Disable Easter Egg Forever",
            Message = "Hide the easter egg permanently? This cannot be undone.",
            OkText = "Hide Forever",
            CancelText = "Cancel"
        });

        if (result)
            _easterEggService.DisableForever();
    }
}