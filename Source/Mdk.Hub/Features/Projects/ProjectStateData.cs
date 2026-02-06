using System;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects;

/// <summary>
///     Immutable state data for currently selected project and capabilities.
/// </summary>
public readonly struct ProjectStateData : IEquatable<ProjectStateData>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectStateData"/> struct.
    /// </summary>
    /// <param name="selectedProject">The currently selected project path.</param>
    /// <param name="canMakeScript">Whether Script projects can be created.</param>
    /// <param name="canMakeMod">Whether Mod projects can be created.</param>
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

    /// <summary>
    ///     Determines whether this instance is equal to another <see cref="ProjectStateData"/>.
    /// </summary>
    /// <param name="other">The other instance to compare.</param>
    /// <returns>True if equal; otherwise, false.</returns>
    public bool Equals(ProjectStateData other) =>
        SelectedProject == other.SelectedProject && CanMakeScript == other.CanMakeScript && CanMakeMod == other.CanMakeMod;

    /// <summary>
    ///     Determines whether this instance is equal to the specified object.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True if equal; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is ProjectStateData other && Equals(other);

    /// <summary>
    ///     Gets the hash code for this instance.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => HashCode.Combine(SelectedProject, CanMakeScript, CanMakeMod);

    /// <summary>
    ///     Determines whether two <see cref="ProjectStateData"/> instances are equal.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True if equal; otherwise, false.</returns>
    public static bool operator ==(ProjectStateData left, ProjectStateData right) => left.Equals(right);

    /// <summary>
    ///     Determines whether two <see cref="ProjectStateData"/> instances are not equal.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True if not equal; otherwise, false.</returns>
    public static bool operator !=(ProjectStateData left, ProjectStateData right) => !left.Equals(right);
}
