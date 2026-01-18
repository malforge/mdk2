namespace Mdk.Hub.Features.Projects.Configuration;

/// <summary>
/// Service for reading MDK project configuration from mdk.ini and mdk.local.ini files.
/// </summary>
public interface IProjectConfigurationService
{
    /// <summary>
    /// Loads and merges configuration from mdk.ini and mdk.local.ini for the specified project.
    /// Local settings override main settings where both are present.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <returns>Merged project configuration, or null if no configuration files found.</returns>
    ProjectConfiguration? LoadConfiguration(string projectPath);
}
