using System;

namespace Mdk.Hub.Features.Snackbars;

/// <summary>
/// Represents an action that can be taken on a snackbar notification.
/// </summary>
public class SnackbarAction
{
    /// <summary>
    /// The text to display for the action.
    /// </summary>
    public required string Text { get; init; }
    
    /// <summary>
    /// The action to execute when clicked. Context parameter can pass data like project path.
    /// </summary>
    public required Action<object?> Action { get; init; }
    
    /// <summary>
    /// Whether this action should close the snackbar when executed.
    /// </summary>
    public bool IsClosingAction { get; init; }
    
    /// <summary>
    /// Optional context data to pass to the action.
    /// </summary>
    public object? Context { get; init; }
}
