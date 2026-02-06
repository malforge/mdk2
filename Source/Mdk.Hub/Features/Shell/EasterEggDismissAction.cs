using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Projects.Actions;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Shell;

/// <summary>
/// Provides action commands to dismiss or disable the easter egg feature.
/// </summary>
[Singleton]
[ViewModelFor<EasterEggDismissActionView>]
public class EasterEggDismissAction : ActionItem
{
    readonly AsyncRelayCommand _disableForeverCommand;
    readonly AsyncRelayCommand _disableForTodayCommand;
    readonly IEasterEggService _easterEggService;
    readonly IShell _shell;

    /// <summary>
    /// Initializes a new instance of the <see cref="EasterEggDismissAction"/> class.
    /// </summary>
    /// <param name="shell">The shell to display confirmation dialogs.</param>
    /// <param name="easterEggService">The easter egg service to control visibility.</param>
    public EasterEggDismissAction(IShell shell, IEasterEggService easterEggService)
    {
        _shell = shell;
        _easterEggService = easterEggService;
        _disableForTodayCommand = new AsyncRelayCommand(DisableForToday);
        _disableForeverCommand = new AsyncRelayCommand(DisableForever);
    }

    /// <summary>
    /// Gets the command to disable the easter egg for today.
    /// </summary>
    public ICommand DisableForTodayCommand => _disableForTodayCommand;

    /// <summary>
    /// Gets the command to disable the easter egg permanently.
    /// </summary>
    public ICommand DisableForeverCommand => _disableForeverCommand;

    /// <summary>
    /// Gets the category of this action item.
    /// </summary>
    public override string Category => "EasterEgg";

    /// <summary>
    /// Determines whether this action should be shown based on easter egg activation status.
    /// </summary>
    /// <returns>True if the easter egg is currently active; otherwise, false.</returns>
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
