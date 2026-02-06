namespace Mdk.Hub.Features.CommonDialogs;

/// <summary>
/// Represents a message configuration for key phrase validation dialogs.
/// </summary>
public readonly struct KeyPhraseValidationMessage()
{
    /// <summary>
    /// Gets or initializes the title displayed in the dialog.
    /// </summary>
    public required string Title { get; init; } = nameof(Title);
    
    /// <summary>
    /// Gets or initializes the message text displayed to the user.
    /// </summary>
    public required string Message { get; init; } = nameof(Message);
    
    /// <summary>
    /// Gets or initializes the watermark text shown in the key phrase input field.
    /// </summary>
    public required string KeyPhraseWatermark { get; init; } = nameof(KeyPhraseWatermark);
    
    /// <summary>
    /// Gets or initializes the key phrase that must be entered for validation to succeed.
    /// </summary>
    public required string RequiredKeyPhrase { get; init; } = nameof(RequiredKeyPhrase);
    
    /// <summary>
    /// Gets or initializes the text for the OK button.
    /// </summary>
    public string OkText { get; init; } = "OK";
    
    /// <summary>
    /// Gets or initializes the text for the Cancel button.
    /// </summary>
    public string CancelText { get; init; } = "Cancel";
}