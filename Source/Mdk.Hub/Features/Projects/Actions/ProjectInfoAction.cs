using Mdk.Hub.Features.Projects.Overview;

namespace Mdk.Hub.Features.Projects.Actions;

public class ProjectInfoAction : ActionItem
{
    public ProjectInfoAction(ProjectModel project)
    {
        Project = project;
    }

    public ProjectModel Project { get; }

    public bool IsScript => Project.Type == ProjectType.IngameScript;

    public string ProjectTypeName => Project.Type == ProjectType.IngameScript 
        ? "Programmable Block Script" 
        : "Mod";

    public override string? Category => "Project";

    public override bool ShouldShow(ProjectListItem? selectedProject, bool canMakeScript, bool canMakeMod)
    {
        // Only show if a project is selected
        return selectedProject is ProjectModel;
    }
}
