namespace Mdk.Hub.Features.Projects;

/// <summary>
///     Indicates how a project was added to the registry.
/// </summary>
public enum ProjectAdditionSource
{
    /// <summary>
    ///     User manually added the project via UI.
    /// </summary>
    Manual,

    /// <summary>
    ///     Project was added via build notification (IPC).
    /// </summary>
    BuildNotification,

    /// <summary>
    ///     Project was loaded from registry on startup.
    /// </summary>
    Startup
}