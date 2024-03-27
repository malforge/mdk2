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
    public required Action<ToastActionArguments> Action { get; init; }

    /// <summary>
    ///     Whether or not this action should close the notification.
    /// </summary>
    public bool IsClosingAction { get; init; }

    /// <summary>
    ///    Whether or not this action should only be available once.
    /// </summary>
    public bool OneTimeOnly { get; init; }
}

public class ToastActionArguments
{
    public Task HoldTask { get; private set; } = Task.CompletedTask;

    public void Hold(Task task)
    {
        HoldTask = task ?? throw new ArgumentNullException(nameof(task));
    }
}