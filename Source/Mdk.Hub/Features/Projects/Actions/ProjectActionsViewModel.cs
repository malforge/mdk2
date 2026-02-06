using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Mal.SourceGeneratedDI;
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

/// <summary>
///     ViewModel managing the actions panel in the Hub, coordinating action display,
///     project options drawer, and context switching between projects.
/// </summary>
[Singleton]
[ViewModelFor<ProjectActionsView>]
public partial class ProjectActionsViewModel : ViewModel
{
    // Registry of action types to resolve
    static readonly Type[] ActionTypes =
    [
        typeof(UpdatesAction),
        typeof(AnnouncementsAction),
        typeof(ProjectManagementAction),
        typeof(ProjectInfoAction),
        typeof(ApiDocsAction),
        typeof(UpdatePackagesAction),
        typeof(EasterEggDismissAction)
    ];

    readonly List<ActionItem> _allActions = new();
    readonly ILogger _logger;
    readonly Dictionary<CanonicalPath, ProjectContext> _projectContexts = new(CanonicalPathComparer.Instance);
    readonly IProjectService _projectService;
    readonly IShell _shell;
    ObservableCollection<ActionItem> _actions = new();
    ProjectOverviewViewModel? _projectOverviewViewModel;

    bool _isOptionsDrawerOpen;

    bool _isUpdatingDisplayedActions;
    bool _updateScheduled;
    bool _pendingRefreshFilters;

    CanonicalPath? _optionsProjectPath;

    ProjectOptionsViewModel? _optionsViewModel;
    ShellViewModel? _shellViewModel;
    UnsavedChangesHandle? _unsavedChangesHandle;

    /// <summary>
    ///     Initializes a new instance of <see cref="ProjectActionsViewModel"/>.
    /// </summary>
    public ProjectActionsViewModel(IShell shell, IProjectService projectService, IEasterEggService easterEggService, ILogger logger)
    {
        _shell = shell;
        _projectService = projectService;
        _logger = logger;
        _projectService.StateChanged += OnProjectStateChanged;
        easterEggService.ActiveChanged += OnEasterEggActiveChanged;

        ShowAboutCommand = new RelayCommand(ShowAbout);
        OpenGlobalSettingsCommand = new RelayCommand(OpenGlobalSettings);
    }

    /// <summary>
    ///     Gets whether a script can be created in the current state.
    /// </summary>
    public bool CanMakeScript => _projectService.State.CanMakeScript;
    
    /// <summary>
    ///     Gets whether a mod can be created in the current state.
    /// </summary>
    public bool CanMakeMod => _projectService.State.CanMakeMod;

    /// <summary>
    ///     Gets or sets whether the project options drawer is currently open.
    /// </summary>
    public bool IsOptionsDrawerOpen
    {
        get => _isOptionsDrawerOpen;
        set => SetProperty(ref _isOptionsDrawerOpen, value);
    }

    /// <summary>
    ///     Gets or sets the path of the project whose options are currently displayed.
    /// </summary>
    public CanonicalPath? OptionsProjectPath
    {
        get => _optionsProjectPath;
        set => SetProperty(ref _optionsProjectPath, value);
    }

    /// <summary>
    ///     Gets or sets the currently displayed project options ViewModel.
    /// </summary>
    public ProjectOptionsViewModel? OptionsViewModel
    {
        get => _optionsViewModel;
        set => SetProperty(ref _optionsViewModel, value);
    }

    /// <summary>
    ///     Gets the collection of currently displayed actions (including separators).
    /// </summary>
    public ObservableCollection<ActionItem> Actions
    {
        get => _actions;
        private set => SetProperty(ref _actions, value);
    }

    /// <summary>
    ///     Gets the command to show the About dialog.
    /// </summary>
    public ICommand ShowAboutCommand { get; }
    
    /// <summary>
    ///     Gets the command to open global settings.
    /// </summary>
    public ICommand OpenGlobalSettingsCommand { get; }

    /// <summary>
    ///     Initializes the ViewModel with the shell instance and handles initial state.
    /// </summary>
    public void Initialize(ShellViewModel shell, ProjectOverviewViewModel overviewViewModel)
    {
        _shellViewModel = shell;
        _projectOverviewViewModel = overviewViewModel;

        // Subscribe to selection changes
        _projectOverviewViewModel.PropertyChanged += OnOverviewPropertyChanged;
        
        // Build all actions once
        BuildAllActions();

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

    /// <summary>
    ///     Checks whether the specified project has unsaved option changes.
    /// </summary>
    public bool HasUnsavedChanges(string projectPath)
    {
        var canonicalPath = new CanonicalPath(projectPath);
        if (_projectContexts.TryGetValue(canonicalPath, out var context) && context.OptionsViewModel != null)
            return context.OptionsViewModel.HasUnsavedChanges;
        return false;
    }

    /// <summary>
    ///     Opens the project options drawer for the specified project.
    /// </summary>
    public void ShowOptionsDrawer(string projectPath)
    {
        OptionsProjectPath = new CanonicalPath(projectPath);

        var canonicalPath = new CanonicalPath(projectPath);

        // Get or create context for options/cache only
        var selectedProjectPath = _projectService.State.SelectedProject;
        if (!selectedProjectPath.IsEmpty() && selectedProjectPath == canonicalPath && _shellViewModel != null)
        {
            // Get the project info to create the model
            var projectInfo = _projectService.GetProjects().FirstOrDefault(p => p.ProjectPath == canonicalPath);
            if (projectInfo != null)
            {
                var projectModel = _shellViewModel.GetOrCreateProjectModel(projectInfo);
                if (!_projectContexts.TryGetValue(canonicalPath, out var context))
                {
                    context = new ProjectContext();
                    _projectContexts[canonicalPath] = context;
                }

                // Reuse cached ViewModel if it exists, otherwise create new
                context.OptionsViewModel ??= new ProjectOptionsViewModel(projectPath, _projectService, _shell, _shell, _logger, saved => CloseOptionsDrawer(projectPath, saved), () => UpdateProjectDirtyState(projectPath));

                OptionsViewModel = context.OptionsViewModel;
                IsOptionsDrawerOpen = true;
            }
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

    /// <summary>
    ///     Closes the options drawer without clearing cached data.
    /// </summary>
    public void CloseOptionsDrawer()
    {
        // Just close the drawer without clearing any cache (ESC or X button)
        IsOptionsDrawerOpen = false;
        OptionsViewModel = null;
        // Don't clear OptionsProjectPath - keep it for potential reopen
    }

    void OnProjectStateChanged(object? sender, EventArgs e)
    {
        // Get or create context for the selected project (only for options/cache management)
        var selectedProjectPath = _projectService.State.SelectedProject;
        if (!selectedProjectPath.IsEmpty() && _shellViewModel != null)
        {
            // Get the project info to create the model
            var projectInfo = _projectService.GetProjects().FirstOrDefault(p => p.ProjectPath == selectedProjectPath);
            if (projectInfo != null)
            {
                var selectedProject = _shellViewModel.GetOrCreateProjectModel(projectInfo);

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
    }

    void OnEasterEggActiveChanged(object? sender, EventArgs e) => UpdateDisplayedActions();

    void BuildAllActions()
    {
        // Clear and dispose old actions
        foreach (var action in _allActions)
        {
            action.ShouldShowChanged -= OnActionShouldShowChanged;
            if (action is IDisposable disposable)
                disposable.Dispose();
        }
        
        _allActions.Clear();

        // Resolve all actions from the registry
        foreach (var actionType in ActionTypes)
        {
            var action = (ActionItem)App.Container.Resolve(actionType);
            action.ShouldShowChanged += OnActionShouldShowChanged;
            _allActions.Add(action);
        }
    }

    void OnActionShouldShowChanged(object? sender, EventArgs e)
    {
        // An action's visibility state changed - update filtered list
        UpdateDisplayedActions();
    }

    void OnOverviewPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProjectOverviewViewModel.SelectedProjects))
        {
            // Selection changed - update all actions
            var selection = _projectOverviewViewModel?.SelectedProjects ?? ImmutableArray<ProjectModel>.Empty;
            foreach (var action in _allActions)
                action.SelectedProjects = selection;
            
            UpdateDisplayedActions();
        }
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
            // Filter actions that should show
            var desiredActions = _allActions.Where(a => a.ShouldShow()).ToList();

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
                    if (currentIndex < Actions.Count && Actions[currentIndex] is CategorySeparator)
                    {
                        // Keep existing separator
                        currentIndex++;
                    }
                    else
                    {
                        // Insert new separator
                        var newSeparator = new CategorySeparator();
                        Actions.Insert(currentIndex, newSeparator);
                        currentIndex++;
                    }
                }
                else
                {
                    // No separator needed - remove one if it exists here
                    if (currentIndex < Actions.Count && Actions[currentIndex] is CategorySeparator)
                    {
                        Actions.RemoveAt(currentIndex);
                        // Don't increment currentIndex
                    }
                }

                // Now handle the action itself
                if (currentIndex < Actions.Count && ReferenceEquals(Actions[currentIndex], desiredAction))
                {
                    // Action matches - keep it
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
                        Actions.RemoveAt(currentIndex);
                        // Don't increment currentIndex, don't increment desiredIndex (retry this desired action)
                        continue;
                    }
                    else
                    {
                        // Current action appears later - insert desired action here
                        Actions.Insert(currentIndex, desiredAction);
                        currentIndex++;
                    }
                }
                else
                {
                    // Need to insert the desired action
                    Actions.Insert(currentIndex, desiredAction);
                    currentIndex++;
                }

                previousCategory = desiredAction.Category;
                desiredIndex++;
            }

            // Remove any remaining items
            while (currentIndex < Actions.Count)
            {
                Actions.RemoveAt(currentIndex);
            }
        }
        finally
        {
            _isUpdatingDisplayedActions = false;
        }
    }

    /// <summary>
    ///     Opens the options drawer for the currently selected project.
    /// </summary>
    public void OpenOptionsDrawer()
    {
        var selectedPath = _projectService.State.SelectedProject;
        if (!selectedPath.IsEmpty())
            ShowOptionsDrawer(selectedPath.Value!);
    }
}
