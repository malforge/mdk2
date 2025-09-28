using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Overview;

[Dependency]
[ViewModelFor<ProjectOverviewView>]
public class ProjectOverviewViewModel : ViewModel
{
    readonly IShell _shell;
    readonly ObservableCollection<ProjectListItem> _projects = new();
    readonly ThrottledAction<string> _throttledSearch;
    bool _filterModsOnly;
    bool _filterScriptsOnly;
    int _filterUpdateDepth;
    IEnumerable<ProjectListItem>? _itemsSource;
    string _searchTerm = string.Empty;
    string _searchText = string.Empty;
    bool _showAll = true;

    public ProjectOverviewViewModel(IShell shell)
    {
        _shell = shell;
        Projects = new ReadOnlyObservableCollection<ProjectListItem>(_projects);
        _throttledSearch = new ThrottledAction<string>(SetSearchTerm, TimeSpan.FromMilliseconds(300));
        ClearSearchCommand = new RelayCommand(ClearSearch);

        // Sample data for design-time
        ItemsSource = new ProjectListItem[]
        {
            new NewProjectModel(),
            new ProjectModel(ProjectType.IngameScript, "My Ingame Script", DateTimeOffset.Now, shell),
            new ProjectModel(ProjectType.Mod, "My Mod", DateTimeOffset.Now.AddDays(-1), shell),
            new ProjectModel(ProjectType.IngameScript, "Another Script", DateTimeOffset.Now.AddDays(-2), shell)
        };
    }

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
            SetProperty(ref _filterModsOnly, value);
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
            SetProperty(ref _filterScriptsOnly, value);
            if (value)
            {
                ShowAll = false;
                FilterModsOnly = false;
            }
        }
    }

    public ICommand ClearSearchCommand { get; }

    public void ClearSearch()
    {
        using var handle = BeginFilterUpdate();
        SearchText = string.Empty;
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
            if (!item.MatchesFilter(_searchTerm, FilterModsOnly, FilterScriptsOnly))
                continue;
            _projects.Add(item);
        }
    }
}