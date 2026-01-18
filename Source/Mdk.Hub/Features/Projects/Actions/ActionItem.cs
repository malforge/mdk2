using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Actions;

public abstract class ActionItem : ViewModel
{
    public abstract string? Category { get; }
    public abstract bool ShouldShow(ProjectListItem? selectedProject, bool canMakeScript, bool canMakeMod);
}
