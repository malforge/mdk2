using System;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Shell;

public abstract class OverlayModel : ViewModel
{
    public EventHandler? Dismissed;

    public void Dismiss() => Dismissed?.Invoke(this, EventArgs.Empty);
}