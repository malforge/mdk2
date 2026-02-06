using System;

namespace Mdk.Hub.Features.Projects.Overview;

/// <summary>
///     Event arguments for when a project should be displayed in the Hub.
/// </summary>
/// <param name="project">The project to show.</param>
public class ShowProjectEventArgs(ProjectModel project) : EventArgs
{
    /// <summary>
    ///     Gets the project that should be displayed.
    /// </summary>
    public ProjectModel Project { get; } = project;
}