using System;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects;

/// <summary>
///     Event arguments for when navigation to a project is requested.
/// </summary>
public class ProjectNavigationRequestedEventArgs : EventArgs
{
    /// <summary>
    ///     Gets the canonical path of the project to navigate to.
    /// </summary>
    public required CanonicalPath ProjectPath { get; init; }
    
    /// <summary>
    ///     Gets a value indicating whether to open the project options view.
    /// </summary>
    public bool OpenOptions { get; init; }
}