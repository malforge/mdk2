using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Projects.Options;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Shell;

namespace Mdk.Hub.Features.Projects.Actions;

/// <summary>
/// Holds all state for a specific project (actions, options viewmodel, cached model, etc).
/// </summary>
class ProjectContext
{
    readonly List<ActionItem> _allActions = new();
    readonly ObservableCollection<ActionItem> _filteredActions = new();
    readonly ProjectModel _project;
    readonly IShell _shell;
    readonly ICommonDialogs _dialogs;
    readonly IProjectService _projectService;
    readonly ProjectActionsViewModel _owner;
    readonly Dictionary<string, ActionItem> _globalActionCache;

    public ProjectContext(ProjectModel project, IShell shell, ICommonDialogs dialogs, IProjectService projectService, ProjectActionsViewModel owner, Dictionary<string, ActionItem> globalActionCache)
    {
        _project = project;
        _shell = shell;
        _dialogs = dialogs;
        _projectService = projectService;
        _owner = owner;
        _globalActionCache = globalActionCache;
        
        FilteredActions = new ReadOnlyObservableCollection<ActionItem>(_filteredActions);
        
        BuildAllActions();
        UpdateFilteredActions();
    }

    public ReadOnlyObservableCollection<ActionItem> FilteredActions { get; }
    
    public ProjectOptionsViewModel? OptionsViewModel { get; set; }
    
    public ProjectModel? CachedModel { get; set; }

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
        
        // Create Project action (if available)
        var availableTypes = new List<ProjectType>();
        if (_owner.CanMakeScript)
            availableTypes.Add(ProjectType.IngameScript);
        if (_owner.CanMakeMod)
            availableTypes.Add(ProjectType.Mod);
        if (availableTypes.Count > 0)
        {
            var createActionKey = typeof(CreateProjectAction).FullName!;
            if (!_globalActionCache.TryGetValue(createActionKey, out var createAction))
            {
                var addExistingAction = new AddExistingProjectAction(_shell, _dialogs, _projectService);
                createAction = new CreateProjectAction(availableTypes, addExistingAction, _projectService, _owner);
                
                // Cache if it's global and subscribe to it once
                if (createAction.IsGlobal)
                {
                    createAction.ShouldShowChanged += OnActionShouldShowChanged;
                    _globalActionCache[createActionKey] = createAction;
                }
            }
            _allActions.Add(createAction);
        }
        
        // Project-specific actions (new instances per project)
        var projectInfoAction = new ProjectInfoAction(_project, _projectService, _dialogs, _shell, _owner);
        projectInfoAction.ShouldShowChanged += OnActionShouldShowChanged;
        _allActions.Add(projectInfoAction);
        
        // API docs action (below project info)
        var apiDocsAction = new ApiDocsAction(_project, _projectService, App.Container.Resolve<Diagnostics.ILogger>());
        apiDocsAction.ShouldShowChanged += OnActionShouldShowChanged;
        _allActions.Add(apiDocsAction);
        
        var updatePackagesAction = new UpdatePackagesAction(_project, _shell, _projectService);
        updatePackagesAction.ShouldShowChanged += OnActionShouldShowChanged;
        _allActions.Add(updatePackagesAction);
        
        // Easter egg action
        var easterEggKey = typeof(EasterEggDismissAction).FullName!;
        if (!_globalActionCache.TryGetValue(easterEggKey, out var easterEggAction))
        {
            easterEggAction = new EasterEggDismissAction(_shell, _dialogs);
            
            // Cache if it's global and subscribe to it once
            if (easterEggAction.IsGlobal)
            {
                easterEggAction.ShouldShowChanged += OnActionShouldShowChanged;
                _globalActionCache[easterEggKey] = easterEggAction;
            }
        }
        _allActions.Add(easterEggAction);
    }

    public void UpdateFilteredActions(bool canMakeScript, bool canMakeMod, bool easterEggActive)
    {
        _filteredActions.Clear();
        
        // Just add the actions that should show
        foreach (var action in _allActions)
        {
            if (action.ShouldShow(_project, canMakeScript, canMakeMod))
            {
                _filteredActions.Add(action);
            }
        }
    }
    
    void UpdateFilteredActions()
    {
        // This overload is called from property changes
        UpdateFilteredActions(_owner.CanMakeScript, _owner.CanMakeMod, _shell.IsEasterEggActive);
    }
}
