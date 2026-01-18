using System.Collections.Generic;
using System.Linq;
using Mdk.Hub.Features.Projects.Overview;

namespace Mdk.Hub.Features.Projects.Actions;

public class CreateProjectAction : ActionItem
{
    public CreateProjectAction(IReadOnlyList<ProjectType> availableTypes)
    {
        AvailableTypes = availableTypes;
        Options = availableTypes.Select(t => new CreateOption(t)).ToList();
    }

    public IReadOnlyList<ProjectType> AvailableTypes { get; }
    public IReadOnlyList<CreateOption> Options { get; }

    public override string? Category => null; // Global actions, no category

    public override bool ShouldShow(ProjectListItem? selectedProject, bool canMakeScript, bool canMakeMod)
    {
        // Always show if we can create anything
        return canMakeScript || canMakeMod;
    }
}

public class CreateOption
{
    public CreateOption(ProjectType projectType)
    {
        ProjectType = projectType;
        Title = projectType == ProjectType.IngameScript 
            ? "New Programmable Block Script" 
            : "New Mod";
        Description = projectType == ProjectType.IngameScript
            ? "Create a new Programmable Block Script project"
            : "Create a new Space Engineers mod project";
    }
    
    public ProjectType ProjectType { get; }
    public string Title { get; }
    public string Description { get; }
}
