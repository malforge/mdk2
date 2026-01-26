using System;
using Mdk.Hub.Features.Projects.Overview;

namespace Mdk.Hub.Features.Projects;

public interface IProjectState
{
    event EventHandler? StateChanged;
    
    ProjectModel? SelectedProject { get; }
    bool CanMakeScript { get; }
    bool CanMakeMod { get; }
    
    void UpdateState(ProjectModel? selectedProject, bool canMakeScript, bool canMakeMod);
}
