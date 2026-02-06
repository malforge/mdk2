using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Shell;

/// <summary>
///     View model for a toast notification message.
/// </summary>
public class ToastMessage : ViewModel
{
    bool _isDismissing;
    
    /// <summary>
    ///     Gets or sets the message text to display.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets whether the toast is currently being dismissed.
    /// </summary>
    public bool IsDismissing
    {
        get => _isDismissing;
        set => SetProperty(ref _isDismissing, value);
    }
}