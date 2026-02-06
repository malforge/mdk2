namespace Mdk.Hub.Features.Projects;

/// <summary>
///     Information about an available package update.
/// </summary>
public record PackageUpdateInfo
{
    /// <summary>
    ///     Gets the package identifier.
    /// </summary>
    public required string PackageId { get; init; }

    /// <summary>
    ///     Gets the currently installed version.
    /// </summary>
    public required string CurrentVersion { get; init; }

    /// <summary>
    ///     Gets the latest available version.
    /// </summary>
    public required string LatestVersion { get; init; }
}