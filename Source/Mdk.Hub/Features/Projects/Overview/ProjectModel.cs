using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects.Overview;

public class ProjectModel : ViewModel
{
    readonly IShell _shell;
    readonly AsyncRelayCommand _deleteCommand;
    readonly IProjectService? _projectService;
    bool _hasUnsavedChanges;
    bool _isSelected;
    DateTimeOffset _lastReferenced;
    string _name;
    bool _needsAttention;
    bool _needsUpdate;
    ICommand? _selectCommand;
    ProjectType _type;
    int _updateCount;

    public ProjectModel(ProjectType type, string name, CanonicalPath projectPath, DateTimeOffset lastReferenced, IShell shell, IProjectService? projectService = null)
    {
        ProjectPath = projectPath;
        _lastReferenced = lastReferenced;
        _shell = shell;
        _projectService = projectService;
        _name = name;
        _type = type;
        _deleteCommand = new AsyncRelayCommand(DeleteAsync, CanDelete);
    }

    public CanonicalPath ProjectPath { get; }

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

    public int UpdateCount
    {
        get => _updateCount;
        set => SetProperty(ref _updateCount, value);
    }

    public bool NeedsUpdate
    {
        get => _needsUpdate;
        set => SetProperty(ref _needsUpdate, value);
    }

    public ICommand DeleteCommand => _deleteCommand;

    public bool CanDelete() => true;

    public async Task DeleteAsync()
    {
        if (!CanDelete())
            return;
        var result = await _shell.ShowAsync(
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
            await _shell.ShowAsync(new ConfirmationMessage
            {
                Title = "Delete Failed",
                Message = $"Failed to delete project: {ex.Message}",
                OkText = "OK",
                CancelText = "Close"
            });
        }
    }

    public bool MatchesFilter(string searchText, bool mustBeMod, bool mustBeScript)
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
