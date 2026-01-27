using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Mal.DependencyInjection;
using Mdk.Hub.Features.About;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Projects.Actions.Items;
using Mdk.Hub.Features.Projects.NewProjectDialog;
using Mdk.Hub.Features.Projects.Options;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Settings;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects.Actions;

[Dependency]
[ViewModelFor<ProjectActionsView>]
public partial class ProjectActionsViewModel : ViewModel
{
    readonly Dictionary<string, ActionItem> _globalActionCache = new(); // Shared action instances
    readonly ILogger _logger;
    readonly Dictionary<CanonicalPath, ProjectContext> _projectContexts = new(CanonicalPathComparer.Instance);
    readonly IProjectService _projectService;
    readonly IShell _shell;
    ImmutableArray<ActionItem> _actions = ImmutableArray<ActionItem>.Empty;
    ProjectContext? _currentContext;

    bool _isOptionsDrawerOpen;

    bool _isUpdatingDisplayedActions;

    CanonicalPath? _optionsProjectPath;

    ProjectOptionsViewModel? _optionsViewModel;
    ShellViewModel? _shellViewModel;
    UnsavedChangesHandle? _unsavedChangesHandle;

    public ProjectActionsViewModel(IShell shell, IProjectService projectService, ILogger logger)
    {
        _shell = shell;
        _projectService = projectService;
        _logger = logger;
        _projectService.StateChanged += OnProjectStateChanged;
        _shell.EasterEggActiveChanged += OnEasterEggActiveChanged;

        ShowAboutCommand = new RelayCommand(ShowAbout);
        OpenGlobalSettingsCommand = new RelayCommand(OpenGlobalSettings);
    }

    public bool CanMakeScript => _projectService.State.CanMakeScript;
    public bool CanMakeMod => _projectService.State.CanMakeMod;

    public bool IsOptionsDrawerOpen
    {
        get => _isOptionsDrawerOpen;
        set => SetProperty(ref _isOptionsDrawerOpen, value);
    }

    public CanonicalPath? OptionsProjectPath
    {
        get => _optionsProjectPath;
        set => SetProperty(ref _optionsProjectPath, value);
    }

    public ProjectOptionsViewModel? OptionsViewModel
    {
        get => _optionsViewModel;
        set => SetProperty(ref _optionsViewModel, value);
    }

    public ImmutableArray<ActionItem> Actions
    {
        get => _actions;
        private set => SetProperty(ref _actions, value);
    }

    public ICommand ShowAboutCommand { get; }
    public ICommand OpenGlobalSettingsCommand { get; }

    public void Initialize(ShellViewModel shell)
    {
        _shellViewModel = shell;

        // Handle initial state now that shell is initialized
        OnProjectStateChanged(null, EventArgs.Empty);
    }

    void ShowAbout()
    {
        var aboutViewModel = new AboutViewModel();
        _shell.AddOverlay(aboutViewModel);
    }

    void OpenGlobalSettings()
    {
        var viewModel = App.Container.Resolve<GlobalSettingsViewModel>();
        _shell.AddOverlay(viewModel);
    }

    public bool HasUnsavedChanges(string projectPath)
    {
        var canonicalPath = new CanonicalPath(projectPath);
        if (_projectContexts.TryGetValue(canonicalPath, out var context) && context.OptionsViewModel != null)
            return context.OptionsViewModel.HasUnsavedChanges;
        return false;
    }

    public void ShowOptionsDrawer(string projectPath)
    {
        OptionsProjectPath = new CanonicalPath(projectPath);

        var canonicalPath = new CanonicalPath(projectPath);

        // Get or create context
        ProjectContext? context = null;
        var selectedProjectPath = _projectService.State.SelectedProject;
        if (!selectedProjectPath.IsEmpty() && selectedProjectPath == canonicalPath && _shellViewModel != null)
        {
            // Get the project info to create the model
            var projectInfo = _projectService.GetProjects().FirstOrDefault(p => p.ProjectPath == canonicalPath);
            if (projectInfo != null)
            {
                var projectModel = _shellViewModel.GetOrCreateProjectModel(projectInfo);
                if (!_projectContexts.TryGetValue(canonicalPath, out context))
                {
                    context = new ProjectContext(projectModel, _shell, _projectService, this, _globalActionCache);
                    _projectContexts[canonicalPath] = context;
                }
            }
        }

        // Reuse cached ViewModel if it exists, otherwise create new
        if (context != null)
        {
            if (context.OptionsViewModel == null)
                context.OptionsViewModel = new ProjectOptionsViewModel(projectPath, _projectService, _shell, _shell, saved => CloseOptionsDrawer(projectPath, saved), () => UpdateProjectDirtyState(projectPath));

            OptionsViewModel = context.OptionsViewModel;
            IsOptionsDrawerOpen = true;
        }
    }

    void UpdateProjectDirtyState(string projectPath)
    {
        // Find the project and update its HasUnsavedChanges flag
        ProjectModel? projectModel = null;

        var canonicalPath = new CanonicalPath(projectPath);

        // Try to get from context first
        if (_projectContexts.TryGetValue(canonicalPath, out var context))
            projectModel = context.CachedModel;

        // If not cached in context, check if it's the currently selected project
        var selectedPath = _projectService.State.SelectedProject;
        if (projectModel == null && !selectedPath.IsEmpty() && selectedPath == canonicalPath && _shellViewModel != null)
        {
            var projectInfo = _projectService.GetProjects().FirstOrDefault(p => p.ProjectPath == canonicalPath);
            if (projectInfo != null)
            {
                projectModel = _shellViewModel.GetOrCreateProjectModel(projectInfo);
                if (context != null)
                    context.CachedModel = projectModel;
            }
        }

        if (projectModel != null)
        {
            var hasChanges = HasUnsavedChanges(projectPath);
            projectModel.HasUnsavedChanges = hasChanges;

            // Update unsaved changes registration
            UpdateUnsavedChangesRegistration();
        }
    }

    void UpdateUnsavedChangesRegistration()
    {
        // Check if any projects have unsaved changes
        var anyUnsavedChanges = _projectContexts.Values.Any(ctx => ctx.OptionsViewModel?.HasUnsavedChanges == true);

        if (anyUnsavedChanges && _unsavedChangesHandle == null)
        {
            // Register unsaved changes - navigate to first project with unsaved changes
            _unsavedChangesHandle = _shell.RegisterUnsavedChanges(
                "You have unsaved changes in project options.",
                () =>
                {
                    // Find first project with unsaved changes
                    var firstUnsaved = _projectContexts
                        .FirstOrDefault(kvp => kvp.Value.OptionsViewModel?.HasUnsavedChanges == true);

                    if (!firstUnsaved.Key.IsEmpty())
                        _projectService.NavigateToProject(firstUnsaved.Key);
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

        // Clear HasUnsavedChanges flag for the current project
        if (_projectContexts.TryGetValue(canonicalPath, out var context) && context.CachedModel != null)
            context.CachedModel.HasUnsavedChanges = false;

        // Update unsaved changes registration
        UpdateUnsavedChangesRegistration();

        // Remove the current project's cached viewmodels (whether saved or cancelled)
        if (context != null)
        {
            context.OptionsViewModel = null;
            context.CachedModel = null;
        }

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
        // Get or create context for the selected project
        var selectedProjectPath = _projectService.State.SelectedProject;
        if (!selectedProjectPath.IsEmpty() && _shellViewModel != null)
        {
            // Get the project info to create the model
            var projectInfo = _projectService.GetProjects().FirstOrDefault(p => p.ProjectPath == selectedProjectPath);
            if (projectInfo != null)
            {
                var selectedProject = _shellViewModel.GetOrCreateProjectModel(projectInfo);

                if (!_projectContexts.TryGetValue(selectedProjectPath, out var context))
                {
                    context = new ProjectContext(selectedProject, _shell, _projectService, this, _globalActionCache);
                    _projectContexts[selectedProjectPath] = context;
                }
                _currentContext = context;

                // Handle drawer logic
                if (IsOptionsDrawerOpen)
                {
                    if (OptionsProjectPath != selectedProject.ProjectPath)
                        ShowOptionsDrawer(selectedProject.ProjectPath.Value!);
                }
                else if (HasUnsavedChanges(selectedProject.ProjectPath.Value!))
                    ShowOptionsDrawer(selectedProject.ProjectPath.Value!);
            }
        }
        else
            _currentContext = null;

        UpdateDisplayedActions();
    }

    void OnEasterEggActiveChanged(object? sender, EventArgs e) => UpdateDisplayedActions();

    public void OnContextActionsChanged()
    {
        // Called by ProjectContext when its filtered actions change
        // Context has already updated its filtered list, just refresh display
        if (!_isUpdatingDisplayedActions)
            UpdateDisplayedActions(false);
    }

    void UpdateDisplayedActions(bool refreshFilters = true)
    {
        if (_isUpdatingDisplayedActions)
            return;

        _isUpdatingDisplayedActions = true;
        try
        {
            // If we don't have a context, set empty list
            if (_currentContext == null)
            {
                Actions = ImmutableArray<ActionItem>.Empty;
                return;
            }

            // Optionally refresh the filtered actions first
            if (refreshFilters)
                _currentContext.UpdateFilteredActions(_projectService.State.CanMakeScript, _projectService.State.CanMakeMod, _shell.IsEasterEggActive);

            // Build new list with separators
            var builder = ImmutableArray.CreateBuilder<ActionItem>();
            string? lastCategory = null;
            var isFirstAction = true;

            foreach (var action in _currentContext.FilteredActions)
            {
                // Add separator if category changed
                if (!isFirstAction && lastCategory != action.Category)
                    builder.Add(new CategorySeparator());

                builder.Add(action);
                lastCategory = action.Category;
                isFirstAction = false;
            }

            // Replace entire list
            Actions = builder.ToImmutable();
        }
        finally
        {
            _isUpdatingDisplayedActions = false;
        }
    }

    public void OpenOptionsDrawer()
    {
        var selectedPath = _projectService.State.SelectedProject;
        if (!selectedPath.IsEmpty())
            ShowOptionsDrawer(selectedPath.Value!);
    }
}