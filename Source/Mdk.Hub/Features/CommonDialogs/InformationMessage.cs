namespace Mdk.Hub.Features.CommonDialogs;

/// <summary>
/// Represents an information dialog message with customizable title, message, and button text.
/// </summary>
public readonly struct InformationMessage()
{
    /// <summary>
    /// Gets or initializes the title displayed in the information dialog.
    /// </summary>
    public required string Title { get; init; } = nameof(Title);
    
    /// <summary>
    /// Gets or initializes the message content displayed in the information dialog.
    /// </summary>
    public required string Message { get; init; } = nameof(Message);
    
    /// <summary>
    /// Gets or initializes the text displayed on the OK button. Default is "OK".
    /// </summary>
    public string OkText { get; init; } = "OK";
}