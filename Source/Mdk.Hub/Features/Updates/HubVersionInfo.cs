using System;

namespace Mdk.Hub.Features.Updates;

/// <summary>
///     Information about available Hub versions.
/// </summary>
public record HubVersionInfo
{
    /// <summary>
    ///     Gets the latest available Hub version.
    /// </summary>
    public required string LatestVersion { get; init; }
    
    /// <summary>
    ///     Gets the download URL for the latest version.
    /// </summary>
    public required string DownloadUrl { get; init; }
    
    /// <summary>
    ///     Gets whether the latest version is a prerelease.
    /// </summary>
    public bool IsPrerelease { get; init; }
    
    /// <summary>
    ///     Gets the release notes for the latest version, if available.
    /// </summary>
    public string? ReleaseNotes { get; init; }
    
    /// <summary>
    ///     Gets when the version information was retrieved.
    /// </summary>
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
}