using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Mal.DependencyInjection;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Settings;
using Mdk.Hub.Features.Snackbars;
using Mdk.Hub.Framework;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects.Overview;

public class ShowProjectEventArgs(ProjectListItem project) : EventArgs
{
    public ProjectListItem Project { get; } = project;
}

[Dependency]
[ViewModelFor<ProjectOverviewView>]
public class ProjectOverviewViewModel : ViewModel
{
    static readonly TimeSpan _selectionCooldown = TimeSpan.FromSeconds(30);
    readonly ICommonDialogs _commonDialogs;
    readonly ObservableCollection<ProjectListItem> _projects = new();
    readonly IProjectService _projectService;
    readonly IProjectState _projectState;
    readonly ISettings _settings;
    readonly ISnackbarService _snackbarService;
    readonly ThrottledAction<string> _throttledSearch;
    ImmutableArray<ProjectListItem> _allProjects;
    bool _filterModsOnly;
    bool _filterScriptsOnly;
    int _filterUpdateDepth;
    DateTimeOffset _lastProjectSelectionTime = DateTimeOffset.MinValue;
    string? _pendingNavigationPath;
    string _searchTerm = string.Empty;
    string _searchText = string.Empty;
    bool _showAll = true;

    public ProjectOverviewViewModel() : this(null!, null!, null!, null!, null!)
    {
        IsDesignMode = true;
    }

    public ProjectOverviewViewModel(ICommonDialogs commonDialogs, IProjectState projectState, IProjectService projectService, ISettings settings, ISnackbarService snackbarService)
    {
        _commonDialogs = commonDialogs;
        _projectState = projectState;
        _projectService = projectService;
        _settings = settings;
        _snackbarService = snackbarService;
        FilteredProjects = new ReadOnlyObservableCollection<ProjectListItem>(_projects);
        _throttledSearch = new ThrottledAction<string>(SetSearchTerm, TimeSpan.FromMilliseconds(300));
        ClearSearchCommand = new RelayCommand(ClearSearch);
        SelectProjectCommand = new RelayCommand<ProjectListItem>(p => SelectProject(p));

        if (IsDesignMode)
        {
            // Sample data for design-time
            AllProjects = ImmutableArray.Create(
                new ProjectListItem[]
                {
                    new ProjectModel(ProjectType.IngameScript, "My Programmable Block Script", new CanonicalPath(@"C:\Projects\MyScript\MyScript.csproj"), DateTimeOffset.Now, commonDialogs),
                    new ProjectModel(ProjectType.Mod, "My Mod", new CanonicalPath(@"C:\Projects\MyMod\MyMod.csproj"), DateTimeOffset.Now.AddDays(-1), commonDialogs),
                    new ProjectModel(ProjectType.IngameScript, "Another Programmable Block Script", new CanonicalPath(@"C:\Projects\AnotherScript\AnotherScript.csproj"), DateTimeOffset.Now.AddDays(-2), commonDialogs)
                });
        }
        else
        {
            LoadProjects();
            RestoreSelectedProject();

            // Subscribe to new project notifications
            _projectService.ProjectAdded += OnProjectAdded;

            // Subscribe to navigation requests
            _projectService.ProjectNavigationRequested += OnProjectNavigationRequested;
        }
    }

    public bool IsDesignMode { get; }

    public ImmutableArray<ProjectListItem> AllProjects
    {
        get => _allProjects;
        set
        {
            if (SetProperty(ref _allProjects, value))
                RefreshWithFilters();
        }
    }

    public ReadOnlyObservableCollection<ProjectListItem> FilteredProjects { get; }


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

    /// <summary>
    ///     Raised when a request is made to show a specific project in the hub.
    /// </summary>
    public event EventHandler<ShowProjectEventArgs>? ShowProjectRequested;

    public void ClearSearch()
    {
        using var handle = BeginFilterUpdate();
        SearchText = string.Empty;
    }

    public void SelectProject(ProjectListItem? project, bool scrollToItem = false)
    {
        if (project == null)
            return;

        // Single selection model - deselect all others
        foreach (var item in _projects)
        {
            if (item != project)
                item.IsSelected = false;
        }

        // Toggle selection on clicked item
        project.IsSelected = !project.IsSelected;

        // Clear needs attention flag when selected
        if (project.IsSelected)
        {
            project.NeedsAttention = false;
            _lastProjectSelectionTime = DateTimeOffset.Now; // Track selection time

            // Only trigger scroll for programmatic navigation, not user clicks
            if (scrollToItem)
                ShowProjectRequested?.Invoke(this, new ShowProjectEventArgs(project));
        }

        // Save selected project path
        if (project.IsSelected && project is ProjectModel model)
            _settings.SetValue("LastSelectedProject", model.ProjectPath);
        else if (!project.IsSelected)
            _settings.SetValue("LastSelectedProject", string.Empty);

        UpdateState();
    }

    void UpdateState()
    {
        var selectedProject = _projects.FirstOrDefault(p => p.IsSelected);
        var canMakeScript = !FilterModsOnly;
        var canMakeMod = !FilterScriptsOnly;

        _projectState.UpdateState(selectedProject, canMakeScript, canMakeMod);
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
        var sorted = _projects.OrderByDescending(p => p is ProjectModel m ? m.LastReferenced : DateTimeOffset.MinValue).ToList();
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
        var existingModels = _allProjects.OfType<ProjectModel>().ToList();

        var viewModels = new List<ProjectListItem>();

        foreach (var project in projects)
        {
            // Try to reuse existing model to preserve UI state
            var existingModel = existingModels.FirstOrDefault(m => !m.ProjectPath.IsEmpty() && project.IsPath(m.ProjectPath.Value!));

            if (existingModel != null)
            {
                // Update properties but keep the same instance
                existingModel.UpdateFromProjectInfo(project);
                viewModels.Add(existingModel);
            }
            else
            {
                // New project - create new model
                viewModels.Add(new ProjectModel(project.Type, project.Name, project.ProjectPath, project.LastReferenced, _commonDialogs, _projectService));
            }
        }

        AllProjects = viewModels.ToImmutableArray();

        // Handle pending navigation request
        if (!string.IsNullOrEmpty(_pendingNavigationPath))
        {
            var canonicalPath = new CanonicalPath(_pendingNavigationPath);
            var project = viewModels.OfType<ProjectModel>().FirstOrDefault(p =>
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
        var project = _projects.FirstOrDefault(p => p is ProjectModel model && model.ProjectPath == canonicalPath);
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
        var newProject = _projects.OfType<ProjectModel>().FirstOrDefault(p =>
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

    void OnProjectNavigationRequested(object? sender, ProjectNavigationRequestedEventArgs e)
    {
        // Find the project in the list
        var project = _projects.OfType<ProjectModel>().FirstOrDefault(p =>
            p.ProjectPath == e.ProjectPath);

        if (project != null)
        {
            // Clear any filters that might hide the project
            ShowAll = true;
            FilterScriptsOnly = false;
            FilterModsOnly = false;
            SearchText = string.Empty;

            // Select the project and scroll to it (programmatic navigation)
            SelectProject(project, true);
        }
        else
        {
            // Project not found yet - might be in process of being added
            // Store path for delayed selection after next LoadProjects()
            _pendingNavigationPath = e.ProjectPath.Value;
        }
    }
}