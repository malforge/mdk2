using System;
using System.Collections.Immutable;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Actions;

/// <summary>
///     Base class for all action items displayed in the Hub's actions panel.
///     Actions can be global (shared across all projects) or per-project.
/// </summary>
public abstract class ActionItem : ViewModel
{
    ImmutableArray<ProjectModel> _selectedProjects = ImmutableArray<ProjectModel>.Empty;

    /// <summary>
    ///     The category this action belongs to, or null for top-level actions.
    ///     Actions with the same category are grouped together with a separator.
    /// </summary>
    public abstract string? Category { get; }

    /// <summary>
    ///     Whether this action should be shared across all project contexts (true)
    ///     or created per-project (false). Global actions cannot receive project instances.
    /// </summary>
    public virtual bool IsGlobal => false;

    /// <summary>
    ///     The currently selected projects. Per-project actions use this to know which project(s) they're operating on.
    ///     Single-select: array contains 0-1 items. Multi-select: array contains 0-N items.
    /// </summary>
    public ImmutableArray<ProjectModel> SelectedProjects
    {
        get => _selectedProjects;
        set
        {
            if (SetProperty(ref _selectedProjects, value))
                OnSelectedProjectsChanged();
        }
    }

    /// <summary>
    ///     Raised when the visibility state of this action has changed.
    /// </summary>
    public event EventHandler? ShouldShowChanged;

    /// <summary>
    ///     Whether this action should be visible in the current state.
    ///     Each action determines its own visibility using injected services.
    ///     Default implementation hides the action if more than 1 project is selected (not yet supported).
    /// </summary>
    public virtual bool ShouldShow()
    {
        // Default: hide if multiple projects selected (no actions support multi-select yet)
        return SelectedProjects.Length <= 1;
    }

    /// <summary>
    ///     Called when SelectedProjects changes. Override to react to selection changes.
    /// </summary>
    protected virtual void OnSelectedProjectsChanged()
    {
        // Default: trigger visibility check
        RaiseShouldShowChanged();
    }

    /// <summary>
    ///     Called when user explicitly requests a refresh (e.g., Ctrl+R).
    ///     Override to implement refresh behavior for this action.
    /// </summary>
    public virtual void Refresh()
    {
        // Default: no-op
    }

    /// <summary>
    ///     Raises the <see cref="ShouldShowChanged"/> event to indicate visibility state has changed.
    /// </summary>
    protected void RaiseShouldShowChanged() => ShouldShowChanged?.Invoke(this, EventArgs.Empty);
}
