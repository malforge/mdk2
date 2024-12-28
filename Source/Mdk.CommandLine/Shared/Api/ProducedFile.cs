namespace Mdk.CommandLine.Shared.Api;

/// <summary>
/// Represents a file that was produced by the script producer.
/// </summary>
/// <param name="id"></param>
/// <param name="path"></param>
/// <param name="content"></param>
public readonly struct ProducedFile(string? id, string path, string? content)
{
    /// <summary>
    /// The ID of the produced file.
    /// </summary>
    public string? Id { get; } = id;
        
    /// <summary>
    /// The absolute path of the produced file.
    /// </summary>
    public string Path { get; } = path;
        
    /// <summary>
    /// The content of a produced file, unless it was simply copied from another location.
    /// </summary>
    public string? Content { get; } = content;
}