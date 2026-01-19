using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Mal.DependencyInjection;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Settings;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Overview;

[Dependency]
[ViewModelFor<ProjectOverviewView>]
public class ProjectOverviewViewModel : ViewModel
{
    readonly ICommonDialogs _commonDialogs;
    readonly ObservableCollection<ProjectListItem> _projects = new();
    readonly IProjectService _projectService;
    readonly IProjectState _projectState;
    readonly ISettings _settings;
    readonly ThrottledAction<string> _throttledSearch;
    bool _filterModsOnly;
    bool _filterScriptsOnly;
    int _filterUpdateDepth;
    IEnumerable<ProjectListItem>? _itemsSource;
    string _searchTerm = string.Empty;
    string _searchText = string.Empty;
    bool _showAll = true;

    public ProjectOverviewViewModel() : this(null!, null!, null!, null!)
    {
        IsDesignMode = true;
    }

    public ProjectOverviewViewModel(ICommonDialogs commonDialogs, IProjectState projectState, IProjectService projectService, ISettings settings)
    {
        _commonDialogs = commonDialogs;
        _projectState = projectState;
        _projectService = projectService;
        _settings = settings;
        Projects = new ReadOnlyObservableCollection<ProjectListItem>(_projects);
        _throttledSearch = new ThrottledAction<string>(SetSearchTerm, TimeSpan.FromMilliseconds(300));
        ClearSearchCommand = new RelayCommand(ClearSearch);
        SelectProjectCommand = new RelayCommand<ProjectListItem>(SelectProject);

        if (IsDesignMode)
        {
            // Sample data for design-time
            ItemsSource = new ProjectListItem[]
            {
                new ProjectModel(ProjectType.IngameScript, "My Programmable Block Script", @"C:\Projects\MyScript\MyScript.csproj", DateTimeOffset.Now, commonDialogs, null!),
                new ProjectModel(ProjectType.Mod, "My Mod", @"C:\Projects\MyMod\MyMod.csproj", DateTimeOffset.Now.AddDays(-1), commonDialogs, null!),
                new ProjectModel(ProjectType.IngameScript, "Another Programmable Block Script", @"C:\Projects\AnotherScript\AnotherScript.csproj", DateTimeOffset.Now.AddDays(-2), commonDialogs, null!)
            };
        }
        else
        {
            LoadProjects();
            RestoreSelectedProject();
        }
    }

    public bool IsDesignMode { get; private set; }

    public IEnumerable<ProjectListItem>? ItemsSource
    {
        get => _itemsSource;
        set
        {
            if (SetProperty(ref _itemsSource, value))
                RefreshWithFilters();
        }
    }

    public ReadOnlyObservableCollection<ProjectListItem> Projects { get; }

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

    public void ClearSearch()
    {
        using var handle = BeginFilterUpdate();
        SearchText = string.Empty;
    }

    void SelectProject(ProjectListItem? project)
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
        _projects.Clear();
        if (ItemsSource == null)
            return;
        foreach (var item in ItemsSource)
        {
            if (!item.MatchesFilter(_searchTerm, FilterScriptsOnly, FilterModsOnly))
                continue;
            item.SelectCommand = SelectProjectCommand;
            _projects.Add(item);
        }
    }

    void LoadProjects()
    {
        var projects = _projectService.GetProjects();
        var viewModels = new List<ProjectListItem>();
        
        foreach (var project in projects)
        {
            viewModels.Add(new ProjectModel(project.Type, project.Name, project.ProjectPath, project.LastReferenced, _commonDialogs, _projectService));
        }
        
        ItemsSource = viewModels;
    }

    void RestoreSelectedProject()
    {
        var lastSelectedPath = _settings.GetValue("LastSelectedProject", string.Empty);
        if (string.IsNullOrEmpty(lastSelectedPath))
            return;

        var project = _projects.FirstOrDefault(p => p is ProjectModel model && model.ProjectPath == lastSelectedPath);
        if (project != null)
        {
            project.IsSelected = true;
            UpdateState();
        }
    }
}