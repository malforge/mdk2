using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Mdk.Hub.Features.CommonDialogs;

namespace Mdk.Hub.Features.Projects.Overview;

public class ProjectModel : ProjectListItem
{
    readonly ICommonDialogs _commonDialogs;
    readonly IProjectService? _projectService;
    readonly AsyncRelayCommand _deleteCommand;
    DateTimeOffset _lastReferenced;
    string _name;
    string _projectPath;
    ProjectType _type;

    public ProjectModel(ProjectType type, string name, string projectPath, DateTimeOffset lastReferenced, ICommonDialogs commonDialogs, IProjectService? projectService = null)
    {
        _lastReferenced = lastReferenced;
        _commonDialogs = commonDialogs;
        _projectService = projectService;
        _name = name;
        _projectPath = projectPath;
        _type = type;
        _deleteCommand = new AsyncRelayCommand(DeleteAsync, CanDelete);
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

    public string ProjectPath
    {
        get => _projectPath;
        private set => SetProperty(ref _projectPath, value);
    }

    public DateTimeOffset LastReferenced
    {
        get => _lastReferenced;
        private set => SetProperty(ref _lastReferenced, value);
    }

    public ICommand DeleteCommand => _deleteCommand;

    public bool CanDelete() => true;

    public async Task DeleteAsync()
    {
        if (!CanDelete())
            return;
        var result = await _commonDialogs.ShowAsync(
            new KeyPhraseValidationMessage
            {
                Title = "Delete Project",
                Message = $"Delete project \"{Name}\"? This permanently deletes the C# project itself, not only its hub registration. This action cannot be undone.",
                KeyPhraseWatermark = $"Type \"{Name}\" here to confirm",
                RequiredKeyPhrase = Name,
                OkText = "Delete"
            });
        if (!result)
            return;

        try
        {
            // Delete the project directory
            var projectDirectory = Path.GetDirectoryName(ProjectPath);
            if (!string.IsNullOrEmpty(projectDirectory) && Directory.Exists(projectDirectory))
            {
                Directory.Delete(projectDirectory, recursive: true);
            }

            // Remove from registry
            _projectService?.RemoveProject(ProjectPath);
        }
        catch (Exception ex)
        {
            await _commonDialogs.ShowAsync(new ConfirmationMessage
            {
                Title = "Delete Failed",
                Message = $"Failed to delete project: {ex.Message}",
                OkText = "OK",
                CancelText = "Close"
            });
        }
    }

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
