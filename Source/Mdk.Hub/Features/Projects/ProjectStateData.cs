using System;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects;

/// <summary>
///     Immutable state data for currently selected project and capabilities.
/// </summary>
public readonly struct ProjectStateData : IEquatable<ProjectStateData>
{
    public ProjectStateData(CanonicalPath selectedProject, bool canMakeScript, bool canMakeMod)
    {
        SelectedProject = selectedProject;
        CanMakeScript = canMakeScript;
        CanMakeMod = canMakeMod;
    }

    /// <summary>
    ///     Gets the currently selected project path (check IsEmpty() for no selection).
    /// </summary>
    public CanonicalPath SelectedProject { get; }

    /// <summary>
    ///     Gets whether the user can create Script projects (true if SE or MP is installed).
    /// </summary>
    public bool CanMakeScript { get; }

    /// <summary>
    ///     Gets whether the user can create Mod projects (i.e., SE is installed).
    /// </summary>
    public bool CanMakeMod { get; }

    public bool Equals(ProjectStateData other) =>
        SelectedProject == other.SelectedProject && CanMakeScript == other.CanMakeScript && CanMakeMod == other.CanMakeMod;

    public override bool Equals(object? obj) => obj is ProjectStateData other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(SelectedProject, CanMakeScript, CanMakeMod);

    public static bool operator ==(ProjectStateData left, ProjectStateData right) => left.Equals(right);

    public static bool operator !=(ProjectStateData left, ProjectStateData right) => !left.Equals(right);
}
