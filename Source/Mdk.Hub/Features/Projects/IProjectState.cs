using System;
using Mdk.Hub.Features.Projects.Overview;

namespace Mdk.Hub.Features.Projects;

public interface IProjectState
{
    event EventHandler? StateChanged;
    
    ProjectListItem? SelectedProject { get; }
    bool CanMakeScript { get; }
    bool CanMakeMod { get; }
    
    void UpdateState(ProjectListItem? selectedProject, bool canMakeScript, bool canMakeMod);
}
