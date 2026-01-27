using System;
using System.Threading.Tasks;

namespace Mdk.Hub.Features.Updates;

/// <summary>
/// Service for checking available updates for MDK packages, templates, and the Hub itself.
/// Also handles prerequisite checks and installation.
/// </summary>
public interface IUpdateCheckService
{
    /// <summary>
    /// Registers a callback to be invoked when version check completes.
    /// If already completed, invokes immediately with cached results. Otherwise queues until check completes.
    /// </summary>
    void WhenVersionCheckCompleted(Action<VersionCheckCompletedEventArgs> callback);
    
    /// <summary>
    /// Starts checking for available updates. Protected by reentry guard.
    /// </summary>
    /// <returns>True if check started, false if already in progress.</returns>
    Task<bool> CheckForUpdatesAsync();
    
    /// <summary>
    /// Gets the last known version check results, or null if no check has completed yet.
    /// </summary>
    VersionCheckCompletedEventArgs? LastKnownVersions { get; }
    
    /// <summary>
    /// Checks if the MDK template package is installed.
    /// </summary>
    Task<bool> IsTemplateInstalledAsync();
    
    /// <summary>
    /// Installs the latest MDK template package.
    /// </summary>
    Task InstallTemplateAsync();
    
    /// <summary>
    /// Checks if .NET SDK is installed and returns version info.
    /// </summary>
    /// <returns>Tuple with IsInstalled flag and version string (if installed).</returns>
    Task<(bool IsInstalled, string? Version)> CheckDotNetSdkAsync();
    
    /// <summary>
    /// Downloads and installs the latest .NET SDK.
    /// </summary>
    Task InstallDotNetSdkAsync();
}
