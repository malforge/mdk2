using System;
using System.Threading.Tasks;
using Mdk.Hub.Features.Shell;

namespace Mdk.Hub.Features.CommonDialogs;

public class ToastViewModel : OverlayModel
{
    public ToastViewModel(string message, int durationMs)
    {
        Message = message;
        
        // Auto-dismiss after duration
        _ = Task.Delay(durationMs).ContinueWith(_ => Dismiss());
    }

    public string Message { get; }
}
