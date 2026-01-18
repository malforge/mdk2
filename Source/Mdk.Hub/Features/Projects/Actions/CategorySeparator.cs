using Mdk.Hub.Features.Projects.Overview;

namespace Mdk.Hub.Features.Projects.Actions;

public class CategorySeparator : ActionItem
{
    public override string? Category => null;

    public override bool ShouldShow(ProjectListItem? selectedProject, bool canMakeScript, bool canMakeMod)
    {
        return true; // Separator visibility is controlled by ViewModel
    }
}
