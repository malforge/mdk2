using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects.Configuration;

/// <summary>
///     Contains all data loaded from a project's configuration files and project file.
/// </summary>
public class ProjectData
{
    /// <summary>
    /// The name of the project. This is not necessarily the same as the name of the .csproj file, and is not guaranteed to be unique. It is intended for display purposes only.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    ///     The raw INI data from mdk.ini (null if file doesn't exist).
    /// </summary>
    public Ini? MainIni { get; init; }

    /// <summary>
    ///     The raw INI data from mdk.local.ini (null if file doesn't exist).
    /// </summary>
    public Ini? LocalIni { get; init; }

    /// <summary>
    ///     Path to the mdk.ini file (if it exists).
    /// </summary>
    public string? MainIniPath { get; init; }

    /// <summary>
    ///     Path to the mdk.local.ini file (if it exists).
    /// </summary>
    public string? LocalIniPath { get; init; }

    /// <summary>
    ///     Path to the project (.csproj) file.
    /// </summary>
    public required CanonicalPath ProjectPath { get; init; }

    /// <summary>
    ///     The layered project configuration, as loaded from <see cref="MainIni" /> and <see cref="LocalIni" />.
    /// </summary>
    public required ProjectConfig Config { get; init; }
}