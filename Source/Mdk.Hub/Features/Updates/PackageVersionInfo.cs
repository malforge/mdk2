using System;

namespace Mdk.Hub.Features.Updates;

/// <summary>
///     Information about available package versions.
/// </summary>
public record PackageVersionInfo
{
    /// <summary>
    ///     Gets the package identifier.
    /// </summary>
    public required string PackageId { get; init; }
    
    /// <summary>
    ///     Gets the latest available version.
    /// </summary>
    public required string LatestVersion { get; init; }
    
    /// <summary>
    ///     Gets when the version information was retrieved.
    /// </summary>
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
}