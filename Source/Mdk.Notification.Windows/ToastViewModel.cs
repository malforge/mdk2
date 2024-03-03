namespace Mdk.Notification.Windows;

/// <summary>
///     A view model for a notification.
/// </summary>
public class ToastViewModel : Model
{
    public required string Message { get; init; }
    public required IReadOnlyList<ToastAction> Actions { get; init; }
}