namespace Mdk.Hub.Features.Updates;

/// <summary>
///     Progress information for update operations.
/// </summary>
public record UpdateProgress
{
    /// <summary>
    ///     Gets the current progress message.
    /// </summary>
    public required string Message { get; init; }
    
    /// <summary>
    ///     Gets the completion percentage (0-100), or null if indeterminate.
    /// </summary>
    public double? PercentComplete { get; init; }
}