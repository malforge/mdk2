using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Mal.DependencyInjection;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Projects.Options;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

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
    readonly Dictionary<string, ProjectOptionsViewModel> _cachedOptionsViewModels = new();
    readonly Dictionary<string, ProjectModel> _projectModelCache = new();
    
    [ObservableProperty]
    bool _isOptionsDrawerOpen;
    
    [ObservableProperty]
    string? _optionsProjectPath;
    
    [ObservableProperty]
    ProjectOptionsViewModel? _optionsViewModel;

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
        return _cachedOptionsViewModels.TryGetValue(projectPath, out var viewModel) && viewModel.HasUnsavedChanges;
    }
    
    public void ShowOptionsDrawer(string projectPath)
    {
        OptionsProjectPath = projectPath;
        
        // Reuse cached ViewModel if it exists, otherwise create new
        if (!_cachedOptionsViewModels.TryGetValue(projectPath, out var viewModel))
        {
            viewModel = new ProjectOptionsViewModel(projectPath, _projectService, saved => CloseOptionsDrawer(projectPath, saved), () => UpdateProjectDirtyState(projectPath));
            _cachedOptionsViewModels[projectPath] = viewModel;
        }
        
        OptionsViewModel = viewModel;
        IsOptionsDrawerOpen = true;
    }
    
    void UpdateProjectDirtyState(string projectPath)
    {
        // Find the project and update its HasUnsavedChanges flag
        ProjectModel? projectModel = null;
        
        // Try to get from cache first
        if (!_projectModelCache.TryGetValue(projectPath, out projectModel))
        {
            // If not cached, check if it's the currently selected project
            if (_projectState.SelectedProject is ProjectModel selected && selected.ProjectPath == projectPath)
            {
                projectModel = selected;
                _projectModelCache[projectPath] = projectModel;
            }
        }
        
        if (projectModel != null)
        {
            bool hasChanges = HasUnsavedChanges(projectPath);
            projectModel.HasUnsavedChanges = hasChanges;
            _shell.SetProjectUnsavedState(projectPath, hasChanges);
        }
    }
    
    void CloseOptionsDrawer(string projectPath, bool saved)
    {
        IsOptionsDrawerOpen = false;
        
        // Clear HasUnsavedChanges flag and cache for the current project only
        if (_projectModelCache.TryGetValue(projectPath, out var projectModel))
        {
            projectModel.HasUnsavedChanges = false;
        }
        
        // Update shell
        _shell.SetProjectUnsavedState(projectPath, false);
        
        // Remove the current project from cache (whether saved or cancelled)
        _cachedOptionsViewModels.Remove(projectPath);
        _projectModelCache.Remove(projectPath);
        
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
                    ShowOptionsDrawer(selectedProject.ProjectPath);
                }
            }
            // If drawer is closed and selected project has unsaved changes, open the drawer
            else if (HasUnsavedChanges(selectedProject.ProjectPath))
            {
                ShowOptionsDrawer(selectedProject.ProjectPath);
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
            allActions.Add(new CreateProjectAction(availableTypes, addExistingAction));
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
}
