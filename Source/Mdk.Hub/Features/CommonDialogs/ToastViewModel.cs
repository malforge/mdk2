using System.Threading.Tasks;
using Mdk.Hub.Features.Shell;

namespace Mdk.Hub.Features.CommonDialogs;

/// <summary>
///     View model for a toast notification overlay.
/// </summary>
public class ToastViewModel : OverlayModel
{
    /// <summary>
    ///     Initializes a new instance of the ToastViewModel class.
    /// </summary>
    /// <param name="message">The message to display in the toast.</param>
    /// <param name="durationMs">How long to display the toast before auto-dismissing, in milliseconds.</param>
    public ToastViewModel(string message, int durationMs)
    {
        Message = message;

        // Auto-dismiss after duration
        _ = Task.Delay(durationMs).ContinueWith(_ => Dismiss());
    }

    /// <summary>
    ///     Gets the message displayed in the toast.
    /// </summary>
    public string Message { get; }
}
