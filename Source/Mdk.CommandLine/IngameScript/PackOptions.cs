namespace Mdk.CommandLine.IngameScript;

public readonly struct PackOptions
{
    public MinifierLevel MinifierLevel { get; init; }
    public bool TrimUnusedTypes { get; init; }
    public required string? ProjectFile { get; init; }
    public string? Output { get; init; }
    public bool ToClipboard { get; init; }
    public bool ListProcessors { get; init; }
}