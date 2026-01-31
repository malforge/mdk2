using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

[Singleton]
[ViewModelFor<ProjectActionsView>]
public partial class ProjectActionsViewModel : ViewModel
{
    readonly Dictionary<string, ActionItem> _globalActionCache = new(); // Shared action instances
    readonly ILogger _logger;
    readonly Dictionary<CanonicalPath, ProjectContext> _projectContexts = new(CanonicalPathComparer.Instance);
    readonly IProjectService _projectService;
    readonly IShell _shell;
    ObservableCollection<ActionItem> _actions = new();
    ProjectContext? _currentContext;

    bool _isOptionsDrawerOpen;

    bool _isUpdatingDisplayedActions;
    bool _updateScheduled;
    bool _pendingRefreshFilters;

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

    public ObservableCollection<ActionItem> Actions
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
                    context = new ProjectContext(projectModel, this, _globalActionCache);
                    _projectContexts[canonicalPath] = context;
                }
            }
        }

        // Reuse cached ViewModel if it exists, otherwise create new
        if (context != null)
        {
            context.OptionsViewModel ??= new ProjectOptionsViewModel(projectPath, _projectService, _shell, _shell, saved => CloseOptionsDrawer(projectPath, saved), () => UpdateProjectDirtyState(projectPath));

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
                    context = new ProjectContext(selectedProject, this, _globalActionCache);
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

        // Debounce: Schedule update for next dispatcher tick
        if (_updateScheduled)
        {
            // Already scheduled - just accumulate the refresh flag
            _pendingRefreshFilters = _pendingRefreshFilters || refreshFilters;
            return;
        }

        _updateScheduled = true;
        _pendingRefreshFilters = refreshFilters;

        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            _updateScheduled = false;
            PerformDisplayedActionsUpdate(_pendingRefreshFilters);
        });
    }

    void PerformDisplayedActionsUpdate(bool refreshFilters)
    {
        if (_isUpdatingDisplayedActions)
            return;

        _isUpdatingDisplayedActions = true;
        try
        {
            Debug.WriteLine("=== UpdateDisplayedActions START ===");
            
            // If we don't have a context, clear list
            if (_currentContext == null)
            {
                Debug.WriteLine("No context, clearing actions");
                Actions.Clear();
                return;
            }

            // Optionally refresh the filtered actions first
            if (refreshFilters)
                _currentContext.UpdateFilteredActions();

            // Build desired list - ONLY actions, no separators (no allocation needed)
            var desiredActions = _currentContext.FilteredActions;
            Debug.WriteLine($"Desired actions: {desiredActions.Count}, Current list (with separators): {Actions.Count} items");

            // Sync Actions collection by walking through desired actions and handling category transitions
            int currentIndex = 0;
            int desiredIndex = 0;
            string? previousCategory = null;

            while (desiredIndex < desiredActions.Count)
            {
                var desiredAction = desiredActions[desiredIndex];
                var needsSeparatorBefore = desiredIndex > 0 && previousCategory != desiredAction.Category && desiredAction.Category != null;

                // Handle separator before this action if needed
                if (needsSeparatorBefore)
                {
                    // Check if current item is a separator
                    if (currentIndex < Actions.Count && Actions[currentIndex] is CategorySeparator existingSeparator)
                    {
                        // Keep existing separator
                        Debug.WriteLine($"[{currentIndex}] KEEP: {GetItemDebugName(existingSeparator)} (before {desiredAction.Category})");
                        currentIndex++;
                    }
                    else
                    {
                        // Insert new separator
                        var newSeparator = new CategorySeparator();
                        Debug.WriteLine($"[{currentIndex}] INSERT: [Separator] (before {desiredAction.Category})");
                        Actions.Insert(currentIndex, newSeparator);
                        currentIndex++;
                    }
                }
                else
                {
                    // No separator needed - remove one if it exists here
                    if (currentIndex < Actions.Count && Actions[currentIndex] is CategorySeparator unwantedSeparator)
                    {
                        Debug.WriteLine($"[{currentIndex}] REMOVE: {GetItemDebugName(unwantedSeparator)} (no longer needed)");
                        Actions.RemoveAt(currentIndex);
                        // Don't increment currentIndex
                    }
                }

                // Now handle the action itself
                if (currentIndex < Actions.Count && ReferenceEquals(Actions[currentIndex], desiredAction))
                {
                    // Action matches - keep it
                    Debug.WriteLine($"[{currentIndex}] KEEP: {GetItemDebugName(desiredAction)}");
                    currentIndex++;
                }
                else if (currentIndex < Actions.Count && Actions[currentIndex] is not CategorySeparator)
                {
                    // Current item is a different action - need to check if we should remove or insert
                    var currentAction = Actions[currentIndex];
                    var currentActionIndexInDesired = desiredActions.FindIndex(desiredIndex + 1, a => ReferenceEquals(a, currentAction));

                    if (currentActionIndexInDesired == -1)
                    {
                        // Current action not in desired list - remove it
                        Debug.WriteLine($"[{currentIndex}] REMOVE: {GetItemDebugName(currentAction)} (not in desired list)");
                        Actions.RemoveAt(currentIndex);
                        // Don't increment currentIndex, don't increment desiredIndex (retry this desired action)
                        continue;
                    }
                    else
                    {
                        // Current action appears later - insert desired action here
                        Debug.WriteLine($"[{currentIndex}] INSERT: {GetItemDebugName(desiredAction)} (current action appears later)");
                        Actions.Insert(currentIndex, desiredAction);
                        currentIndex++;
                    }
                }
                else
                {
                    // Need to insert the desired action
                    Debug.WriteLine($"[{currentIndex}] INSERT: {GetItemDebugName(desiredAction)}");
                    Actions.Insert(currentIndex, desiredAction);
                    currentIndex++;
                }

                previousCategory = desiredAction.Category;
                desiredIndex++;
            }

            // Remove any remaining items (actions or separators)
            while (currentIndex < Actions.Count)
            {
                var itemToRemove = Actions[currentIndex];
                Debug.WriteLine($"[{currentIndex}] REMOVE: {GetItemDebugName(itemToRemove)} (beyond desired list)");
                Actions.RemoveAt(currentIndex);
            }

            Debug.WriteLine($"=== UpdateDisplayedActions END (Final count: {Actions.Count}) ===");
        }
        finally
        {
            _isUpdatingDisplayedActions = false;
        }
    }

    static string GetItemDebugName(ActionItem item)
    {
        return item switch
        {
            CategorySeparator => $"[Separator#{item.GetHashCode():X}]",
            _ => $"{item.GetType().Name}#{item.GetHashCode():X}"
        };
    }

    public void OpenOptionsDrawer()
    {
        var selectedPath = _projectService.State.SelectedProject;
        if (!selectedPath.IsEmpty())
            ShowOptionsDrawer(selectedPath.Value!);
    }
}