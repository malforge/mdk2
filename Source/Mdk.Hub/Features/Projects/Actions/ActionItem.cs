using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Framework;

using System;

namespace Mdk.Hub.Features.Projects.Actions;

public abstract class ActionItem : ViewModel
{
    public abstract string? Category { get; }
    
    /// <summary>
    /// Whether this action should be shared across all project contexts (true)
    /// or created per-project (false). Global actions cannot receive a project instance.
    /// </summary>
    public virtual bool IsGlobal => false;
    
    /// <summary>
    /// Raised when the visibility state of this action has changed.
    /// </summary>
    public event EventHandler? ShouldShowChanged;
    
    public abstract bool ShouldShow(ProjectListItem? selectedProject, bool canMakeScript, bool canMakeMod);
    
    protected void RaiseShouldShowChanged()
    {
        ShouldShowChanged?.Invoke(this, EventArgs.Empty);
    }
}
