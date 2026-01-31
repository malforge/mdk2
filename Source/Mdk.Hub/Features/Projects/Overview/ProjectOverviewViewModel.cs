using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Settings;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects.Overview;

public class ShowProjectEventArgs(ProjectModel project) : EventArgs
{
    public ProjectModel Project { get; } = project;
}

[Dependency]
[ViewModelFor<ProjectOverviewView>]
public class ProjectOverviewViewModel : ViewModel
{
    static readonly TimeSpan _selectionCooldown = TimeSpan.FromSeconds(30);
    readonly ILogger _logger;
    readonly ObservableCollection<ProjectModel> _projects = new();
    readonly IProjectService _projectService;
    readonly ISettings _settings;
    readonly ThrottledAction<string> _throttledSearch;
    ImmutableArray<ProjectModel> _allProjects;
    bool _filterModsOnly;
    bool _filterScriptsOnly;
    int _filterUpdateDepth;
    DateTimeOffset _lastProjectSelectionTime = DateTimeOffset.MinValue;
    string? _pendingNavigationPath;
    string _searchTerm = string.Empty;
    string _searchText = string.Empty;
    ShellViewModel? _shellViewModel;
    bool _showAll = true;

    public ProjectOverviewViewModel() : this(null!, null!, null!, null!)
    {
        IsDesignMode = true;
    }

    public ProjectOverviewViewModel(IShell shell, IProjectService projectService, ISettings settings, ILogger logger)
    {
        _projectService = projectService;
        _settings = settings;
        _logger = logger;
        FilteredProjects = new ReadOnlyObservableCollection<ProjectModel>(_projects);
        _throttledSearch = new ThrottledAction<string>(SetSearchTerm, TimeSpan.FromMilliseconds(300));
        ClearSearchCommand = new RelayCommand(ClearSearch);
        SelectProjectCommand = new RelayCommand<ProjectModel>(p => SelectProject(p));

        if (IsDesignMode)
        {
            // Sample data for design-time
            AllProjects = ImmutableArray.Create(
                new[]
                {
                    new ProjectModel(ProjectType.IngameScript, "My Programmable Block Script", new CanonicalPath(@"C:\Projects\MyScript\MyScript.csproj"), DateTimeOffset.Now, shell),
                    new ProjectModel(ProjectType.Mod, "My Mod", new CanonicalPath(@"C:\Projects\MyMod\MyMod.csproj"), DateTimeOffset.Now.AddDays(-1), shell),
                    new ProjectModel(ProjectType.IngameScript, "Another Programmable Block Script", new CanonicalPath(@"C:\Projects\AnotherScript\AnotherScript.csproj"), DateTimeOffset.Now.AddDays(-2), shell)
                });
        }
    }

    public bool IsDesignMode { get; }

    public ImmutableArray<ProjectModel> AllProjects
    {
        get => _allProjects;
        set
        {
            if (SetProperty(ref _allProjects, value))
                RefreshWithFilters();
        }
    }

    private ShellViewModel ShellViewModel => _shellViewModel ?? throw new InvalidOperationException("ProjectOverviewViewModel is not initialized with ShellViewModel");

    public ReadOnlyObservableCollection<ProjectModel> FilteredProjects { get; }


    public string SearchText
    {
        get => _searchText;
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        set
        {
            SetProperty(ref _searchText, value ?? string.Empty);
            _throttledSearch.Invoke(_searchText);
        }
    }

    public bool ShowAll
    {
        get => _showAll;
        set
        {
            using var handle = BeginFilterUpdate();
            SetProperty(ref _showAll, value);
            if (value)
            {
                FilterModsOnly = false;
                FilterScriptsOnly = false;
            }
        }
    }

    public bool FilterModsOnly
    {
        get => _filterModsOnly;
        set
        {
            using var handle = BeginFilterUpdate();
            if (SetProperty(ref _filterModsOnly, value))
                UpdateState();
            if (value)
            {
                ShowAll = false;
                FilterScriptsOnly = false;
            }
        }
    }

    public bool FilterScriptsOnly
    {
        get => _filterScriptsOnly;
        set
        {
            using var handle = BeginFilterUpdate();
            if (SetProperty(ref _filterScriptsOnly, value))
                UpdateState();
            if (value)
            {
                ShowAll = false;
                FilterModsOnly = false;
            }
        }
    }

    public ICommand ClearSearchCommand { get; }

    public ICommand SelectProjectCommand { get; }

    public void Initialize(ShellViewModel shell)
    {
        _shellViewModel = shell;

        if (!IsDesignMode)
        {
            // Subscribe to events
            _projectService.ProjectAdded += OnProjectAdded;
            _projectService.ProjectRemoved += OnProjectRemoved;
            _projectService.ProjectUpdateAvailable += OnProjectUpdateAvailable;
            _projectService.ProjectNavigationRequested += OnProjectNavigationRequested;

            // Load and display projects
            LoadProjects();
            RestoreSelectedProject();
        }
    }

    /// <summary>
    ///     Raised when a request is made to show a specific project in the hub.
    /// </summary>
    public event EventHandler<ShowProjectEventArgs>? ShowProjectRequested;

    public void ClearSearch()
    {
        using var handle = BeginFilterUpdate();
        SearchText = string.Empty;
    }

    void SelectProject(ProjectModel? project, bool scrollToItem = false)
    {
        if (project == null)
            return;

        // Update UI selection
        SelectProjectInList(project, scrollToItem);

        // Update service state (this will trigger StateChanged, but our handler ignores if already selected)
        UpdateState();
    }

    void SelectProjectInList(ProjectModel? project, bool scrollToItem = false)
    {
        if (project == null)
            return;

        // Single selection model - deselect all others
        foreach (var item in _projects)
        {
            if (item != project)
                item.IsSelected = false;
        }

        // Select the project (don't toggle when called from state sync)
        project.IsSelected = true;

        // Clear needs attention flag when selected
        project.NeedsAttention = false;
        _lastProjectSelectionTime = DateTimeOffset.Now; // Track selection time

        // Only trigger scroll for programmatic navigation, not user clicks
        if (scrollToItem)
            ShowProjectRequested?.Invoke(this, new ShowProjectEventArgs(project));

        // Save selected project path
        _settings.SetValue("LastSelectedProject", project.ProjectPath);
    }

    void UpdateState()
    {
        var selectedProject = _projects.FirstOrDefault(p => p.IsSelected);
        var canMakeScript = !FilterModsOnly;
        var canMakeMod = !FilterScriptsOnly;

        _projectService.State = new ProjectStateData(
            selectedProject?.ProjectPath ?? default,
            canMakeScript,
            canMakeMod);
    }

    void SetSearchTerm(string searchTerm)
    {
        using var handle = BeginFilterUpdate();
        _searchTerm = searchTerm;
    }

    IDisposable BeginFilterUpdate()
    {
        _filterUpdateDepth++;
        return new DisposableHandle(EndFilterUpdate);
    }

    void EndFilterUpdate()
    {
        _filterUpdateDepth--;
        if (_filterUpdateDepth < 0)
            throw new InvalidOperationException("Mismatched Begin/EndFilterUpdate calls");
        if (_filterUpdateDepth == 0)
            RefreshWithFilters();
    }

    void RefreshWithFilters()
    {
        if (_allProjects.IsDefaultOrEmpty)
        {
            _projects.Clear();
            return;
        }

        // Remember currently selected project to restore after refresh
        var selectedProject = _projects.FirstOrDefault(p => p.IsSelected);

        // Build new filtered list
        var filteredItems = _allProjects
            .Where(item => item.MatchesFilter(_searchTerm, FilterScriptsOnly, FilterModsOnly))
            .ToList();

        // Set select command
        foreach (var item in filteredItems)
            item.SelectCommand = SelectProjectCommand;

        // Update collection without clearing (to minimize visual disruption)
        // Remove items that are no longer in filtered list
        for (var i = _projects.Count - 1; i >= 0; i--)
        {
            if (!filteredItems.Contains(_projects[i]))
                _projects.RemoveAt(i);
        }

        // Add new items that aren't already in the collection
        foreach (var item in filteredItems)
        {
            if (!_projects.Contains(item))
                _projects.Add(item);
        }

        // Re-sort in place (minimizes UI disruption compared to clear+add)
        var sorted = _projects.OrderByDescending(p => p.LastReferenced).ToList();
        for (var i = 0; i < sorted.Count; i++)
        {
            var currentIndex = _projects.IndexOf(sorted[i]);
            if (currentIndex != i)
                _projects.Move(currentIndex, i);
        }

        // Restore selection if the selected project is still in the list
        if (selectedProject != null && _projects.Contains(selectedProject))
            selectedProject.IsSelected = true;
    }

    void LoadProjects()
    {
        var projects = _projectService.GetProjects();

        var viewModels = new List<ProjectModel>();

        foreach (var project in projects)
        {
            // Get shared ProjectModel from ShellViewModel - this is the ONLY way to get instances
            var model = ShellViewModel.GetOrCreateProjectModel(project);
            viewModels.Add(model);
        }

        AllProjects = viewModels.ToImmutableArray();

        // Handle pending navigation request
        if (!string.IsNullOrEmpty(_pendingNavigationPath))
        {
            var canonicalPath = new CanonicalPath(_pendingNavigationPath);
            var project = viewModels.FirstOrDefault(p =>
                p.ProjectPath == canonicalPath);

            if (project != null)
            {
                // Clear filters
                ShowAll = true;
                FilterScriptsOnly = false;
                FilterModsOnly = false;
                SearchText = string.Empty;

                // Select the project and scroll to it (pending navigation)
                SelectProject(project, true);
                _pendingNavigationPath = null;
            }
        }
    }

    void RestoreSelectedProject()
    {
        var lastSelectedPath = _settings.GetValue("LastSelectedProject", string.Empty);
        if (string.IsNullOrEmpty(lastSelectedPath))
            return;

        var canonicalPath = new CanonicalPath(lastSelectedPath);
        var project = _projects.FirstOrDefault(p => p.ProjectPath == canonicalPath);
        if (project != null)
        {
            project.IsSelected = true;
            UpdateState();
        }
    }

    void OnProjectAdded(object? sender, ProjectAddedEventArgs e)
    {
        // Ignore startup imports - those are loaded in bulk
        if (e.Source == ProjectAdditionSource.Startup)
            return;

        // Refresh the project list to include the new project
        LoadProjects();

        // Find the newly added project
        var newProject = _projects.FirstOrDefault(p =>
            p.ProjectPath == e.ProjectPath);

        if (newProject == null)
            return;

        // Check if user is actively working (selected a project recently)
        var timeSinceLastSelection = DateTimeOffset.Now - _lastProjectSelectionTime;
        var userIsActivelyWorking = timeSinceLastSelection < _selectionCooldown;

        if (userIsActivelyWorking)
        {
            // User is busy - just mark for attention, don't interrupt
            newProject.NeedsAttention = true;
        }
        else
        {
            // User hasn't selected anything recently - auto-select the new project
            // (No need to set NeedsAttention since SelectProject clears it anyway)
            SelectProject(newProject);
        }
    }

    public void NavigateToProject(CanonicalPath projectPath)
    {
        // Find the project in the ALL projects list
        var project = _projects.FirstOrDefault(p =>
            p.ProjectPath == projectPath);

        if (project != null)
        {
            // Check if project is visible in the current filtered list
            var isVisibleInFilteredList = FilteredProjects.Any(p =>
                p.ProjectPath == projectPath);

            // Only clear filters if the project is not visible with current filters
            if (!isVisibleInFilteredList)
            {
                ShowAll = true;
                FilterScriptsOnly = false;
                FilterModsOnly = false;
                SearchText = string.Empty;
            }

            // Select the project and scroll to it (programmatic navigation)
            SelectProject(project, true);
        }
        else
        {
            // Project not found yet - might be in process of being added
            // Store path for delayed selection after next LoadProjects()
            _pendingNavigationPath = projectPath.Value;
        }
    }

    void OnProjectRemoved(object? sender, CanonicalPath projectPath) =>
        // Refresh the project list to remove the deleted project
        LoadProjects();

    void OnProjectNavigationRequested(object? sender, ProjectNavigationRequestedEventArgs e)
    {
        // Handle explicit navigation request - can adjust filters if needed
        var projectPath = e.ProjectPath;

        // Find the project in the ALL projects list
        var project = _projects.FirstOrDefault(p => p.ProjectPath == projectPath);

        if (project != null)
        {
            // Check if project is visible in the current filtered list
            var isVisibleInFilteredList = FilteredProjects.Any(p => p.ProjectPath == projectPath);

            // Only clear filters if the project is not visible with current filters
            if (!isVisibleInFilteredList)
            {
                ShowAll = true;
                FilterScriptsOnly = false;
                FilterModsOnly = false;
                SearchText = string.Empty;
            }

            // Update UI selection (don't update state - ProjectService already did)
            SelectProjectInList(project, true);
        }
        else
        {
            // Project not found yet - might be in process of being added
            // Store path for delayed selection after next LoadProjects()
            _pendingNavigationPath = projectPath.Value;
        }
    }

    void OnProjectUpdateAvailable(object? sender, ProjectUpdateAvailableEventArgs e)
    {
        _logger.Info($"Update available for project: {e.ProjectPath}");

        // Find the project model and update its status
        var projectModel = AllProjects.OfType<ProjectModel>().FirstOrDefault(p => p.ProjectPath == e.ProjectPath);
        if (projectModel != null)
        {
            if (e.AvailableUpdates.Count == 0)
            {
                // Empty update list means clear the update state
                _logger.Info($"Clearing update state for project {projectModel.Name}");
                projectModel.NeedsUpdate = false;
                projectModel.UpdateCount = 0;
            }
            else
            {
                _logger.Info($"Project {projectModel.Name} has {e.AvailableUpdates.Count} update(s) available");
                projectModel.NeedsUpdate = true;
                projectModel.UpdateCount = e.AvailableUpdates.Count;
            }
        }
        else
            _logger.Warning($"Could not find project model for {e.ProjectPath}");
    }
}