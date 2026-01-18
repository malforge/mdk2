using System;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Projects.Overview;

namespace Mdk.Hub.Features.Projects;

[Dependency<IProjectState>]
public class ProjectState : IProjectState
{
    public event EventHandler? StateChanged;
    
    public ProjectListItem? SelectedProject { get; private set; }
    public bool CanMakeScript { get; private set; } = true;
    public bool CanMakeMod { get; private set; } = true;
    
    public void UpdateState(ProjectListItem? selectedProject, bool canMakeScript, bool canMakeMod)
    {
        SelectedProject = selectedProject;
        CanMakeScript = canMakeScript;
        CanMakeMod = canMakeMod;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
