using System;

namespace Mdk.Hub.Features.Updates;

/// <summary>
///     Information about available template versions.
/// </summary>
public record TemplateVersionInfo
{
    /// <summary>
    ///     Gets the latest available template version.
    /// </summary>
    public required string LatestVersion { get; init; }
    
    /// <summary>
    ///     Gets when the version information was retrieved.
    /// </summary>
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
}