using System;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Actions;

public abstract class ActionItem : ViewModel
{
    ProjectModel? _project;

    public abstract string? Category { get; }

    /// <summary>
    ///     Whether this action should be shared across all project contexts (true)
    ///     or created per-project (false). Global actions cannot receive a project instance.
    /// </summary>
    public virtual bool IsGlobal => false;

    /// <summary>
    ///     The currently selected project. Per-project actions use this to know which project they're operating on.
    /// </summary>
    public ProjectModel? Project
    {
        get => _project;
        set
        {
            if (SetProperty(ref _project, value))
                OnSelectedProjectChanged();
        }
    }

    /// <summary>
    ///     Raised when the visibility state of this action has changed.
    /// </summary>
    public event EventHandler? ShouldShowChanged;

    /// <summary>
    ///     Whether this action should be visible in the current state.
    ///     Each action determines its own visibility using injected services.
    /// </summary>
    public abstract bool ShouldShow();

    /// <summary>
    ///     Called when the SelectedProject changes. Override to react to project selection changes.
    /// </summary>
    protected virtual void OnSelectedProjectChanged()
    {
        // Default: trigger visibility check
        RaiseShouldShowChanged();
    }

    protected void RaiseShouldShowChanged() => ShouldShowChanged?.Invoke(this, EventArgs.Empty);
}