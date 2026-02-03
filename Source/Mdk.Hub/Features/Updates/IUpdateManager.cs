using System;
using System.Threading;
using System.Threading.Tasks;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Updates;

/// <summary>
///     Centralized service for checking and executing all types of updates (Hub, Templates, NuGet packages).
///     Replaces IUpdateCheckService with expanded update execution capabilities.
/// </summary>
public interface IUpdateManager
{
    /// <summary>
    ///     Gets the last known version check results, or null if no check has completed yet.
    /// </summary>
    VersionCheckCompletedEventArgs? LastKnownVersions { get; }

    /// <summary>
    ///     Registers a callback to be invoked when version check completes.
    ///     If already completed, invokes immediately with cached results. Otherwise queues until check completes.
    /// </summary>
    void WhenVersionCheckUpdates(Action<VersionCheckCompletedEventArgs> callback);

    /// <summary>
    ///     Starts checking for available updates. Protected by reentry guard.
    /// </summary>
    /// <returns>True if check started, false if already in progress.</returns>
    Task<bool> CheckForUpdatesAsync();

    /// <summary>
    ///     Checks if the MDK template package is installed.
    /// </summary>
    Task<bool> IsTemplateInstalledAsync();

    /// <summary>
    ///     Installs the latest MDK template package.
    /// </summary>
    Task InstallTemplateAsync();

    /// <summary>
    ///     Checks if .NET SDK is installed and returns version info.
    /// </summary>
    /// <returns>Tuple with IsInstalled flag and version string (if installed).</returns>
    Task<(bool IsInstalled, string? Version)> CheckDotNetSdkAsync();

    /// <summary>
    ///     Downloads and installs the latest .NET SDK.
    /// </summary>
    Task InstallDotNetSdkAsync();

    // Update execution methods

    /// <summary>
    ///     Updates the Hub application to the latest version using Velopack.
    /// </summary>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Update result indicating success/failure.</returns>
    Task<UpdateResult> UpdateHubAsync(IProgress<UpdateProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates the MDK script templates to the latest version.
    /// </summary>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Update result indicating success/failure.</returns>
    Task<UpdateResult> UpdateTemplatesAsync(IProgress<UpdateProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates NuGet packages for a specific project.
    /// </summary>
    /// <param name="projectPath">Path to the project file.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Update result indicating success/failure.</returns>
    Task<UpdateResult> UpdateProjectPackagesAsync(CanonicalPath projectPath, IProgress<UpdateProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a project has a backup file for rollback.
    /// </summary>
    /// <param name="projectPath">Path to the project file.</param>
    /// <returns>True if rollback is possible, false otherwise.</returns>
    Task<bool> CanRollbackProjectAsync(CanonicalPath projectPath);

    /// <summary>
    ///     Rolls back NuGet package updates for a specific project by restoring from backup.
    /// </summary>
    /// <param name="projectPath">Path to the project file.</param>
    /// <returns>Update result indicating success/failure.</returns>
    Task<UpdateResult> RollbackProjectPackagesAsync(CanonicalPath projectPath);
}

