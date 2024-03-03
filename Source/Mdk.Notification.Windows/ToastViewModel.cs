namespace Mdk.Notification.Windows;

/// <summary>
///     A view model for a notification.
/// </summary>
public class ToastViewModel : Model
{
    /// <summary>
    ///    The message to display in the notification.
    /// </summary>
    public required string Message { get; init; }
    
    /// <summary>
    ///    The actions to display in the notification.
    /// </summary>
    public required IReadOnlyList<ToastAction> Actions { get; init; }
}