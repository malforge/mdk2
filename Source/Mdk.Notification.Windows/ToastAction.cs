namespace Mdk.Notification.Windows;

/// <summary>
///     A notification action.
/// </summary>
public readonly struct ToastAction
{
    /// <summary>
    ///     The text to display for the action.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    ///     The action to perform when the action is clicked.
    /// </summary>
    public required Action Action { get; init; }

    /// <summary>
    ///     Whether or not this action should close the notification.
    /// </summary>
    public bool IsClosingAction { get; init; }
}