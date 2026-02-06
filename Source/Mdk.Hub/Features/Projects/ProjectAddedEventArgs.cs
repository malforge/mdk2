using System;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects;

/// <summary>
///     Event arguments for when a new project is added to the registry.
/// </summary>
public class ProjectAddedEventArgs : EventArgs
{
    /// <summary>
    ///     Gets the canonical path of the project that was added.
    /// </summary>
    public required CanonicalPath ProjectPath { get; init; }
    
    /// <summary>
    ///     Gets the source from which the project was added.
    /// </summary>
    public ProjectAdditionSource Source { get; init; }
}