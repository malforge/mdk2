using System;
using System.Collections.ObjectModel;
using Mal.DependencyInjection;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Overview;

[Dependency]
[ViewModelFor<ProjectOverviewView>]
public class ProjectOverviewViewModel : ViewModel
{
    string _searchText = string.Empty;
    bool _showAll = true;
    bool _filterModsOnly;
    bool _filterScriptsOnly;

    public ProjectOverviewViewModel()
    {
        // Sample data for design-time
        Projects.Add(new ProjectModel(ProjectType.IngameScript, "My Ingame Script", DateTimeOffset.Now));
        Projects.Add(new ProjectModel(ProjectType.Mod, "My Mod", DateTimeOffset.Now.AddDays(-1)));
        Projects.Add(new ProjectModel(ProjectType.IngameScript, "Another Script", DateTimeOffset.Now.AddDays(-2)));
    }

    public ObservableCollection<ProjectModel> Projects { get; } = new();

    public string SearchText
    {
        get => _searchText;
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        set => SetProperty(ref _searchText, value ?? string.Empty);
    }

    public bool ShowAll
    {
        get => _showAll;
        set
        {
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
            SetProperty(ref _filterScriptsOnly, value);
            if (value)
            {
                ShowAll = false;
                FilterModsOnly = false;
            }
        }
    }
}