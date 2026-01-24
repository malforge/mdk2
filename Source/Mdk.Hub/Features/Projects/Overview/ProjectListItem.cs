using System.Windows.Input;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Overview;

public abstract class ProjectListItem: ViewModel
{
    bool _isSelected;
    bool _needsAttention;
    ICommand? _selectCommand;
    
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    public bool NeedsAttention
    {
        get => _needsAttention;
        set => SetProperty(ref _needsAttention, value);
    }
    
    public ICommand? SelectCommand
    {
        get => _selectCommand;
        set => SetProperty(ref _selectCommand, value);
    }
    
    public abstract bool MatchesFilter(string searchText, bool mustBeMod, bool mustBeScript);
}