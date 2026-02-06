using System;
using System.Collections.Generic;

namespace Mdk.Hub.Features.Updates;

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