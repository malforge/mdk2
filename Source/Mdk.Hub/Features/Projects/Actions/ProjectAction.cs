using Mdk.Hub.Features.Projects.Overview;

namespace Mdk.Hub.Features.Projects.Actions;

public class ProjectAction : ActionItem
{
    public ProjectAction(string title, string actionType)
    {
        Title = title;
        ActionType = actionType;
    }

    public string Title { get; }
    public string ActionType { get; }

    public override string? Category => "Project";

    public override bool ShouldShow(ProjectListItem? selectedProject, bool canMakeScript, bool canMakeMod)
    {
        // Only show if a project is selected
        return selectedProject is ProjectModel;
    }
}
