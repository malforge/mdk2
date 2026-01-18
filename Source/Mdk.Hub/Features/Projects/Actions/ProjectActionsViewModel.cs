using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Actions;

[Dependency]
[ViewModelFor<ProjectActionsView>]
public class ProjectActionsViewModel : ViewModel
{
    readonly ObservableCollection<ActionItem> _actions = new();
    readonly IProjectState _projectState;

    public ProjectActionsViewModel(IProjectState projectState)
    {
        _projectState = projectState;
        _projectState.StateChanged += OnProjectStateChanged;
        Actions = new ReadOnlyObservableCollection<ActionItem>(_actions);
        
        UpdateActions();
    }

    public ReadOnlyObservableCollection<ActionItem> Actions { get; }

    void OnProjectStateChanged(object? sender, EventArgs e)
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
            allActions.Add(new CreateProjectAction(availableTypes));

        // Project-specific actions
        if (_projectState.SelectedProject is ProjectModel projectModel)
        {
            allActions.Add(new ProjectInfoAction(projectModel));
        }

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
