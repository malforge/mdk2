namespace Mdk.Hub.Features.CommonDialogs;

/// <summary>
/// Represents a confirmation dialog message with customizable title, message, and button text.
/// </summary>
public readonly struct ConfirmationMessage()
{
    /// <summary>
    /// Gets or initializes the title displayed in the confirmation dialog.
    /// </summary>
    public required string Title { get; init; } = nameof(Title);
    
    /// <summary>
    /// Gets or initializes the message content displayed in the confirmation dialog.
    /// </summary>
    public required string Message { get; init; } = nameof(Message);
    
    /// <summary>
    /// Gets or initializes the text displayed on the OK button. Default is "OK".
    /// </summary>
    public string OkText { get; init; } = "OK";
    
    /// <summary>
    /// Gets or initializes the text displayed on the Cancel button. Default is "Cancel".
    /// </summary>
    public string CancelText { get; init; } = "Cancel";
}