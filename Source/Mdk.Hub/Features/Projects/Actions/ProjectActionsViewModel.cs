using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Mal.DependencyInjection;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Projects.NewProjectDialog;
using Mdk.Hub.Features.Projects.Options;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects.Actions;

[Dependency]
[ViewModelFor<ProjectActionsView>]
public partial class ProjectActionsViewModel : ViewModel
{
    readonly ObservableCollection<ActionItem> _actions = new();
    readonly IProjectState _projectState;
    readonly IShell _shell;
    readonly ICommonDialogs _dialogs;
    readonly IProjectService _projectService;
    readonly Dictionary<CanonicalPath, ProjectOptionsViewModel> _cachedOptionsViewModels = new(CanonicalPathComparer.Instance);
    readonly Dictionary<CanonicalPath, ProjectModel> _projectModelCache = new(CanonicalPathComparer.Instance);
    UnsavedChangesHandle? _unsavedChangesHandle;
    
    bool _isOptionsDrawerOpen;
    public bool IsOptionsDrawerOpen
    {
        get => _isOptionsDrawerOpen;
        set => SetProperty(ref _isOptionsDrawerOpen, value);
    }
    
    CanonicalPath? _optionsProjectPath;
    public CanonicalPath? OptionsProjectPath
    {
        get => _optionsProjectPath;
        set => SetProperty(ref _optionsProjectPath, value);
    }
    
    ProjectOptionsViewModel? _optionsViewModel;
    public ProjectOptionsViewModel? OptionsViewModel
    {
        get => _optionsViewModel;
        set => SetProperty(ref _optionsViewModel, value);
    }

    public ProjectActionsViewModel(IProjectState projectState, IShell shell, ICommonDialogs dialogs, IProjectService projectService)
    {
        _projectState = projectState;
        _shell = shell;
        _dialogs = dialogs;
        _projectService = projectService;
        _projectState.StateChanged += OnProjectStateChanged;
        _shell.EasterEggActiveChanged += OnEasterEggActiveChanged;
        Actions = new ReadOnlyObservableCollection<ActionItem>(_actions);
        
        UpdateActions();
    }

    public ReadOnlyObservableCollection<ActionItem> Actions { get; }
    
    public bool HasUnsavedChanges(string projectPath)
    {
        var canonicalPath = new CanonicalPath(projectPath);
        return _cachedOptionsViewModels.TryGetValue(canonicalPath, out var viewModel) && viewModel.HasUnsavedChanges;
    }
    
    public void ShowOptionsDrawer(string projectPath)
    {
        OptionsProjectPath = new CanonicalPath(projectPath);
        
        var canonicalPath = new CanonicalPath(projectPath);
        
        // Reuse cached ViewModel if it exists, otherwise create new
        if (!_cachedOptionsViewModels.TryGetValue(canonicalPath, out var viewModel))
        {
            viewModel = new ProjectOptionsViewModel(projectPath, _projectService, _dialogs, _shell, saved => CloseOptionsDrawer(projectPath, saved), () => UpdateProjectDirtyState(projectPath));
            _cachedOptionsViewModels[canonicalPath] = viewModel;
        }
        
        OptionsViewModel = viewModel;
        IsOptionsDrawerOpen = true;
    }
    
    void UpdateProjectDirtyState(string projectPath)
    {
        // Find the project and update its HasUnsavedChanges flag
        ProjectModel? projectModel = null;
        
        var canonicalPath = new CanonicalPath(projectPath);
        
        // Try to get from cache first
        if (!_projectModelCache.TryGetValue(canonicalPath, out projectModel))
        {
            // If not cached, check if it's the currently selected project
            if (_projectState.SelectedProject is ProjectModel selected && selected.ProjectPath == canonicalPath)
            {
                projectModel = selected;
                _projectModelCache[canonicalPath] = projectModel;
            }
        }
        
        if (projectModel != null)
        {
            bool hasChanges = HasUnsavedChanges(projectPath);
            projectModel.HasUnsavedChanges = hasChanges;
            
            // Update unsaved changes registration
            UpdateUnsavedChangesRegistration();
        }
    }
    
    void UpdateUnsavedChangesRegistration()
    {
        // Check if any projects have unsaved changes
        bool anyUnsavedChanges = _cachedOptionsViewModels.Values.Any(vm => vm.HasUnsavedChanges);
        
        if (anyUnsavedChanges && _unsavedChangesHandle == null)
        {
            // Register unsaved changes - navigate to first project with unsaved changes
            _unsavedChangesHandle = _shell.RegisterUnsavedChanges(
                "You have unsaved changes in project options.",
                () =>
                {
                    // Find first project with unsaved changes
                    var firstUnsaved = _cachedOptionsViewModels
                        .FirstOrDefault(kvp => kvp.Value.HasUnsavedChanges);
                    
                    if (!firstUnsaved.Key.IsEmpty())
                    {
                        _projectService.NavigateToProject(firstUnsaved.Key);
                    }
                });
        }
        else if (!anyUnsavedChanges && _unsavedChangesHandle != null)
        {
            // Dispose handle when no more unsaved changes
            _unsavedChangesHandle.Value.Dispose();
            _unsavedChangesHandle = null;
        }
    }
    
    void CloseOptionsDrawer(string projectPath, bool saved)
    {
        IsOptionsDrawerOpen = false;
        
        var canonicalPath = new CanonicalPath(projectPath);
        
        // Clear HasUnsavedChanges flag and cache for the current project only
        if (_projectModelCache.TryGetValue(canonicalPath, out var projectModel))
        {
            projectModel.HasUnsavedChanges = false;
        }
        
        // Update unsaved changes registration
        UpdateUnsavedChangesRegistration();
        
        // Remove the current project from cache (whether saved or cancelled)
        _cachedOptionsViewModels.Remove(canonicalPath);
        _projectModelCache.Remove(canonicalPath);
        
        OptionsViewModel = null;
        OptionsProjectPath = null;
    }
    
    public void CloseOptionsDrawer()
    {
        // Just close the drawer without clearing any cache (ESC or X button)
        IsOptionsDrawerOpen = false;
        OptionsViewModel = null;
        // Don't clear OptionsProjectPath - keep it for potential reopen
    }

    void OnProjectStateChanged(object? sender, EventArgs e)
    {
        UpdateActions();
        
        if (_projectState.SelectedProject is ProjectModel selectedProject)
        {
            // If drawer is open and a different project is selected, switch to it
            if (IsOptionsDrawerOpen)
            {
                if (OptionsProjectPath != selectedProject.ProjectPath)
                {
                    if (!selectedProject.ProjectPath.IsEmpty())
                        ShowOptionsDrawer(selectedProject.ProjectPath.Value!);
                }
            }
            // If drawer is closed and selected project has unsaved changes, open the drawer
            else if (!selectedProject.ProjectPath.IsEmpty() && HasUnsavedChanges(selectedProject.ProjectPath.Value!))
            {
                ShowOptionsDrawer(selectedProject.ProjectPath.Value!);
            }
        }
    }

    void OnEasterEggActiveChanged(object? sender, EventArgs e)
    {
        UpdateActions();
    }

    void UpdateActions()
    {
        // Create all possible action widgets
        var allActions = new List<ActionItem>();

        // Create options
        var availableTypes = new List<ProjectType>();
        if (_projectState.CanMakeScript)
            availableTypes.Add(ProjectType.IngameScript);
        if (_projectState.CanMakeMod)
            availableTypes.Add(ProjectType.Mod);
        if (availableTypes.Count > 0)
        {
            var addExistingAction = new AddExistingProjectAction(_shell, _dialogs, _projectService);
            allActions.Add(new CreateProjectAction(availableTypes, addExistingAction, _projectService, this));
        }

        // Project-specific actions
        if (_projectState.SelectedProject is ProjectModel projectModel)
        {
            allActions.Add(new ProjectInfoAction(projectModel, _projectService, _dialogs, _shell, this));
        }

        // Easter egg dismiss (always add, will filter by ShouldShow)
        allActions.Add(new EasterEggDismissAction(_shell, _dialogs));

        // Filter by ShouldShow and insert separators between category changes
        _actions.Clear();
        string? lastCategory = null;
        bool isFirstItem = true;
        foreach (var action in allActions)
        {
            if (action.ShouldShow(_projectState.SelectedProject, _projectState.CanMakeScript, _projectState.CanMakeMod))
            {
                // Insert separator if category changed (but not for first item)
                if (!isFirstItem && lastCategory != action.Category)
                    _actions.Add(new CategorySeparator());
                
                _actions.Add(action);
                lastCategory = action.Category;
                isFirstItem = false;
            }
        }
    }

    public async Task<NewProjectDialogResult?> ShowNewProjectDialogAsync(NewProjectDialogMessage message)
    {
        var viewModel = new NewProjectDialogViewModel(message);
        await _shell.ShowOverlayAsync(viewModel);
        return viewModel.Result;
    }

    public async Task ShowBusyOverlayAsync(CommonDialogs.BusyOverlayViewModel busyOverlay)
    {
        await _shell.ShowOverlayAsync(busyOverlay);
    }

    public async Task ShowErrorAsync(string title, string message)
    {
        await _dialogs.ShowAsync(new ConfirmationMessage
        {
            Title = title,
            Message = message,
            OkText = "OK",
            CancelText = "Cancel"
        });
    }

    public void OpenOptionsDrawer()
    {
        if (_projectState.SelectedProject is ProjectModel projectModel && !projectModel.ProjectPath.IsEmpty())
        {
            ShowOptionsDrawer(projectModel.ProjectPath.Value!);
        }
    }
}
