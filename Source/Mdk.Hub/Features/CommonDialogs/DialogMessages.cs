namespace Mdk.Hub.Features.CommonDialogs;

public readonly struct ConfirmationMessage()
{
    public required string Title { get; init; } = nameof(Title);
    public required string Message { get; init; } = nameof(Message);
    public string OkText { get; init; } = "OK";
    public string CancelText { get; init; } = "Cancel";
}

public readonly struct InformationMessage()
{
    public required string Title { get; init; } = nameof(Title);
    public required string Message { get; init; } = nameof(Message);
    public string OkText { get; init; } = "OK";
}

public readonly struct KeyPhraseValidationMessage()
{
    public required string Title { get; init; } = nameof(Title);
    public required string Message { get; init; } = nameof(Message);
    public required string KeyPhraseWatermark { get; init; } = nameof(KeyPhraseWatermark);
    public required string RequiredKeyPhrase { get; init; } = nameof(RequiredKeyPhrase);
    public string OkText { get; init; } = "OK";
    public string CancelText { get; init; } = "Cancel";
}

