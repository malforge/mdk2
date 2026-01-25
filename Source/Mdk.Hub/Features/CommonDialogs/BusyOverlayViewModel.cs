using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.CommonDialogs;

[ViewModelFor<BusyOverlayView>]
public class BusyOverlayViewModel(string message) : OverlayModel
{
    public string Message { get; } = message;
}
