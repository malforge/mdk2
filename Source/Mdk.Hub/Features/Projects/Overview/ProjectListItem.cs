using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Overview;

public abstract class ProjectListItem: ViewModel
{
    public abstract bool MatchesFilter(string searchText, bool mustBeMod, bool mustBeScript);
}