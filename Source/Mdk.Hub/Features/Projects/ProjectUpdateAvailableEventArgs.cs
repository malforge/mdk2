using System;
using System.Collections.Generic;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects;

/// <summary>
///     Event arguments for when a project has updates available.
/// </summary>
public class ProjectUpdateAvailableEventArgs : EventArgs
{
    /// <summary>
    ///     Gets the canonical path of the project that has updates available.
    /// </summary>
    public required CanonicalPath ProjectPath { get; init; }
    
    /// <summary>
    ///     Gets the list of available package updates for the project.
    /// </summary>
    public required IReadOnlyList<PackageUpdateInfo> AvailableUpdates { get; init; }
}