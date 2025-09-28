using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Shell;

namespace Mdk.Hub.Features.Projects.Overview;

public class ProjectModel : ProjectListItem
{
    DateTimeOffset _lastReferenced;
    readonly IShell _shell;
    string _name;
    ProjectType _type;
    readonly AsyncRelayCommand _deleteCommand;

    public ProjectModel(ProjectType type, string name, DateTimeOffset lastReferenced, IShell shell)
    {
        _lastReferenced = lastReferenced;
        _shell = shell;
        _name = name;
        _type = type;
        _deleteCommand = new AsyncRelayCommand(DeleteAsync, CanDelete);
    }

    public bool CanDelete()
    {
        return true;
    }

    public async Task DeleteAsync()
    {
        if (!CanDelete())
            return;
        var result = await _shell.ConfirmDangerousOperationAsync(
            "Delete Project",
            $"Are you sure you want to delete the project '{Name}'? This action cannot be undone.",
            "Type the project name to confirm.",
            Name,
            "Delete", "Cancel");
        if (!result)
            return;
        // TODO delete
    }


    public ProjectType Type
    {
        get => _type;
        private set => SetProperty(ref _type, value);
    }

    public string Name
    {
        get => _name;
        private set => SetProperty(ref _name, value);
    }

    public DateTimeOffset LastReferenced
    {
        get => _lastReferenced;
        private set => SetProperty(ref _lastReferenced, value);
    }

    public ICommand DeleteCommand => _deleteCommand;

    public override bool MatchesFilter(string searchText, bool mustBeMod, bool mustBeScript)
    {
        if (mustBeMod && Type != ProjectType.Mod)
            return false;
        if (mustBeScript && Type != ProjectType.IngameScript)
            return false;
        if (!string.IsNullOrWhiteSpace(searchText) && !Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }
}