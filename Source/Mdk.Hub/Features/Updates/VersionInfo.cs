using System;
using System.Collections.Generic;

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

/// <summary>
///     Event arguments for when version check completes.
/// </summary>
public class VersionCheckCompletedEventArgs : EventArgs
{
    /// <summary>
    ///     Gets the list of available package versions.
    /// </summary>
    public required IReadOnlyList<PackageVersionInfo> Packages { get; init; }
    
    /// <summary>
    ///     Gets the available template version information, if any.
    /// </summary>
    public TemplateVersionInfo? TemplatePackage { get; init; }
    
    /// <summary>
    ///     Gets the available Hub version information, if any.
    /// </summary>
    public HubVersionInfo? HubVersion { get; init; }
}
