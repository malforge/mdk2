using System;
using System.Collections.Generic;

namespace Mdk.Hub.Features.Updates;

/// <summary>
///     Information about available package versions.
/// </summary>
public record PackageVersionInfo
{
    public required string PackageId { get; init; }
    public required string LatestVersion { get; init; }
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
///     Information about available template versions.
/// </summary>
public record TemplateVersionInfo
{
    public required string LatestVersion { get; init; }
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
///     Information about available Hub versions.
/// </summary>
public record HubVersionInfo
{
    public required string LatestVersion { get; init; }
    public required string DownloadUrl { get; init; }
    public string? ReleaseNotes { get; init; }
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
///     Event arguments for when version check completes.
/// </summary>
public class VersionCheckCompletedEventArgs : EventArgs
{
    public required IReadOnlyList<PackageVersionInfo> Packages { get; init; }
    public TemplateVersionInfo? TemplatePackage { get; init; }
    public HubVersionInfo? HubVersion { get; init; }
}