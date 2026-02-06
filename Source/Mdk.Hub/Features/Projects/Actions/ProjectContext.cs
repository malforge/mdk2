using Mdk.Hub.Features.Projects.Options;
using Mdk.Hub.Features.Projects.Overview;

namespace Mdk.Hub.Features.Projects.Actions;

/// <summary>
///     Holds project-specific state (options viewmodel and cached model for unsaved changes tracking).
/// </summary>
class ProjectContext
{
    public ProjectOptionsViewModel? OptionsViewModel { get; set; }

    public ProjectModel? CachedModel { get; set; }
}
