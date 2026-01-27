using Mdk.Hub.Features.Projects.Overview;

namespace Mdk.Hub.Features.Projects.Actions;

public class CategorySeparator : ActionItem
{
    public override string? Category => null;

    public override bool ShouldShow(ProjectModel? selectedProject, bool canMakeScript, bool canMakeMod) => true; // Separator visibility is controlled by ViewModel
}