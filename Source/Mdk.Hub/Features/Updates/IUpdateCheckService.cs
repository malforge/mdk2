using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mdk.Hub.Features.Updates;

/// <summary>
/// Service for checking available versions of MDK packages, templates, and the Hub itself.
/// </summary>
public interface IUpdateCheckService
{
    /// <summary>
    /// Raised when version check completes successfully.
    /// </summary>
    event EventHandler<VersionCheckCompletedEventArgs>? VersionCheckCompleted;
    
    /// <summary>
    /// Triggers a version check. Protected with reentry guard.
    /// </summary>
    /// <returns>True if check was started, false if already running.</returns>
    Task<bool> CheckForUpdatesAsync();
    
    /// <summary>
    /// Gets the last known version information, if available.
    /// </summary>
    VersionCheckCompletedEventArgs? LastKnownVersions { get; }
}
