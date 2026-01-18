using System.Windows.Input;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Overview;

public abstract class ProjectListItem: ViewModel
{
    bool _isSelected;
    ICommand? _selectCommand;
    
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    public ICommand? SelectCommand
    {
        get => _selectCommand;
        set => SetProperty(ref _selectCommand, value);
    }
    
    public abstract bool MatchesFilter(string searchText, bool mustBeMod, bool mustBeScript);
}