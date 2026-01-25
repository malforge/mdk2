using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Framework;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects.Overview;

public class ProjectModel : ProjectListItem
{
    readonly ICommonDialogs _commonDialogs;
    readonly AsyncRelayCommand _deleteCommand;
    readonly IProjectService? _projectService;
    bool _hasUnsavedChanges;
    DateTimeOffset _lastReferenced;
    string _name;
    ProjectType _type;

    public ProjectModel(ProjectType type, string name, CanonicalPath projectPath, DateTimeOffset lastReferenced, ICommonDialogs commonDialogs, IProjectService? projectService = null)
        : base(projectPath)
    {
        _lastReferenced = lastReferenced;
        _commonDialogs = commonDialogs;
        _projectService = projectService;
        _name = name;
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

    public DateTimeOffset LastReferenced
    {
        get => _lastReferenced;
        private set => SetProperty(ref _lastReferenced, value);
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => SetProperty(ref _hasUnsavedChanges, value);
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
            var projectDirectory = Path.GetDirectoryName(ProjectPath.Value);
            if (!string.IsNullOrEmpty(projectDirectory) && Directory.Exists(projectDirectory))
                Directory.Delete(projectDirectory, true);

            // Remove from registry
            if (!ProjectPath.IsEmpty())
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

    /// <summary>
    ///     Updates this model's properties from a ProjectInfo without losing UI state.
    /// </summary>
    public void UpdateFromProjectInfo(ProjectInfo projectInfo)
    {
        Name = projectInfo.Name;
        Type = projectInfo.Type;
        LastReferenced = projectInfo.LastReferenced;
        // Note: ProjectPath, IsSelected, NeedsAttention, HasUnsavedChanges are NOT updated - those preserve UI state
    }
}