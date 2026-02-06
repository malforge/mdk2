using System;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Shell;

/// <summary>
///     Base class for view models that are displayed as overlays in the shell.
/// </summary>
public abstract class OverlayModel : ViewModel
{
    /// <summary>
    ///     Event raised when the overlay is dismissed.
    /// </summary>
    public EventHandler? Dismissed;

    /// <summary>
    ///     Dismisses the overlay by invoking the Dismissed event.
    /// </summary>
    public void Dismiss() => Dismissed?.Invoke(this, EventArgs.Empty);
}
