using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Projects.Actions.Items;
using Mdk.Hub.Features.Projects.Options;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Shell;

namespace Mdk.Hub.Features.Projects.Actions;

/// <summary>
///     Holds all state for a specific project (actions, options viewmodel, cached model, etc).
/// </summary>
class ProjectContext
{
    // Registry of action types to resolve
    static readonly Type[] ActionTypes =
    [
        typeof(ProjectManagementAction),
        typeof(ProjectInfoAction),
        typeof(ApiDocsAction),
        typeof(UpdatePackagesAction),
        typeof(EasterEggDismissAction)
    ];

    readonly List<ActionItem> _allActions = new();
    readonly ObservableCollection<ActionItem> _filteredActions = new();
    readonly Dictionary<string, ActionItem> _globalActionCache;
    readonly ProjectActionsViewModel _owner;
    readonly ProjectModel _project;

    public ProjectContext(ProjectModel project, ProjectActionsViewModel owner, Dictionary<string, ActionItem> globalActionCache)
    {
        _project = project;
        _owner = owner;
        _globalActionCache = globalActionCache;

        FilteredActions = new ReadOnlyObservableCollection<ActionItem>(_filteredActions);

        BuildAllActions();
        UpdateFilteredActions();
    }

    public ReadOnlyObservableCollection<ActionItem> FilteredActions { get; }

    public ProjectOptionsViewModel? OptionsViewModel { get; set; }

    public ProjectModel? CachedModel { get; set; }

    public void OnContextBecameActive()
    {
        // Notify all per-project actions that they should refresh
        // Actions are shared singletons - reassign Project to trigger OnSelectedProjectChanged()
        foreach (var action in _allActions)
        {
            if (!action.IsGlobal)
                action.Project = _project;
        }
    }

    void OnActionShouldShowChanged(object? sender, EventArgs e)
    {
        // An action's visibility state changed - update our filtered list first
        UpdateFilteredActions();
        // Then notify parent that displayed actions need to be refreshed
        _owner.OnContextActionsChanged();
    }

    void BuildAllActions()
    {
        // Unsubscribe and dispose old non-global actions before clearing
        foreach (var action in _allActions)
        {
            if (!action.IsGlobal)
            {
                action.ShouldShowChanged -= OnActionShouldShowChanged;
                if (action is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        _allActions.Clear();

        // Resolve all actions from the registry
        foreach (var actionType in ActionTypes)
        {
            var actionKey = actionType.FullName!;

            // Check if this is a global action that we already have cached
            if (_globalActionCache.TryGetValue(actionKey, out var cachedAction))
            {
                _allActions.Add(cachedAction);
                continue;
            }

            // Resolve the action from DI
            var action = (ActionItem)App.Container.Resolve(actionType);

            // Set project for per-project actions
            if (!action.IsGlobal)
                action.Project = _project;

            // Subscribe to should show changes
            action.ShouldShowChanged += OnActionShouldShowChanged;

            // Cache global actions for reuse
            if (action.IsGlobal)
                _globalActionCache[actionKey] = action;

            _allActions.Add(action);
        }
    }

    public void UpdateFilteredActions()
    {
        _filteredActions.Clear();

        // Just add the actions that should show
        foreach (var action in _allActions)
        {
            if (action.ShouldShow())
                _filteredActions.Add(action);
        }
    }
}