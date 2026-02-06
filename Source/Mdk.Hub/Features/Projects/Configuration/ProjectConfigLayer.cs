using System;
using System.Collections.Immutable;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects.Configuration;

/// <summary>
/// Represents a single layer of project configuration settings. Multiple layers can be combined to form the final effective configuration.
/// </summary>
public class ProjectConfigLayer : IEquatable<ProjectConfigLayer>
{
    /// <summary>
    ///     Gets or initializes the project type (ProgrammableBlock or Mod).
    /// </summary>
    public ProjectType? Type { get; init; }

    /// <summary>
    ///     Gets or initializes the interactive mode setting for how users interact with builds.
    /// </summary>
    public InteractiveMode? Interactive { get; init; }

    /// <summary>
    ///     Gets or initializes whether to enable trace logging.
    /// </summary>
    public bool? Trace { get; init; }

    /// <summary>
    ///     Gets or initializes the minification level to apply to scripts.
    /// </summary>
    public MinifierLevel? Minify { get; init; }

    /// <summary>
    ///     Gets or initializes extra minification options when minifying.
    /// </summary>
    public MinifierExtraOptions? MinifyExtraOptions { get; init; }

    /// <summary>
    ///     Gets or initializes a list of glob patterns for files to ignore during builds.
    /// </summary>
    public ImmutableArray<string>? Ignores { get; init; }

    /// <summary>
    ///     Gets or initializes a list of namespaces that analyzers should accept without warnings (scripts only).
    /// </summary>
    public ImmutableArray<string>? Namespaces { get; init; }

    /// <summary>
    ///     Gets or initializes the output path for built scripts/mods. Null means "auto" (determine automatically).
    /// </summary>
    public CanonicalPath? Output { get; init; }

    /// <summary>
    ///     Gets or initializes the path to Space Engineers binaries. Null means "auto" (determine automatically).
    /// </summary>
    public CanonicalPath? BinaryPath { get; init; }

    /// <summary>
    ///     Compares two configuration layers for semantic equality (uses type-aware comparison for collections and paths).
    /// </summary>
    /// <param name="other">The configuration layer to compare with.</param>
    /// <returns>True if the layers are semantically equal, false otherwise.</returns>
    public bool Equals(ProjectConfigLayer? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Type == other.Type
               && Interactive == other.Interactive
               && Trace == other.Trace
               && Minify == other.Minify
               && MinifyExtraOptions == other.MinifyExtraOptions
               && ProjectConfig.CompareIgnores(Ignores, other.Ignores)
               && ProjectConfig.CompareNamespaces(Namespaces, other.Namespaces)
               && ProjectConfig.ComparePaths(Output, other.Output)
               && ProjectConfig.ComparePaths(BinaryPath, other.BinaryPath);
    }

    /// <summary>
    ///     Determines whether the specified object is equal to the current configuration layer.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if the objects are equal, false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ProjectConfigLayer)obj);
    }

    /// <summary>
    ///     Returns a hash code for this configuration layer.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Type);
        hashCode.Add(Interactive);
        hashCode.Add(Trace);
        hashCode.Add(Minify);
        hashCode.Add(MinifyExtraOptions);
        hashCode.Add(Ignores);
        hashCode.Add(Namespaces);
        hashCode.Add(Output);
        hashCode.Add(BinaryPath);
        return hashCode.ToHashCode();
    }

    /// <summary>
    ///     Determines whether two configuration layers are equal.
    /// </summary>
    /// <param name="left">The first layer to compare.</param>
    /// <param name="right">The second layer to compare.</param>
    /// <returns>True if the layers are equal, false otherwise.</returns>
    public static bool operator ==(ProjectConfigLayer? left, ProjectConfigLayer? right) => Equals(left, right);

    /// <summary>
    ///     Determines whether two configuration layers are not equal.
    /// </summary>
    /// <param name="left">The first layer to compare.</param>
    /// <param name="right">The second layer to compare.</param>
    /// <returns>True if the layers are not equal, false otherwise.</returns>
    public static bool operator !=(ProjectConfigLayer? left, ProjectConfigLayer? right) => !Equals(left, right);
}