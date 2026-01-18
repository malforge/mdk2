using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Overview;

public abstract class ProjectListItem: ViewModel
{
    bool _isSelected;
    
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    public abstract bool MatchesFilter(string searchText, bool mustBeMod, bool mustBeScript);
}