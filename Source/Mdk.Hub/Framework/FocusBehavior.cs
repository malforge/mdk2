using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Mdk.Hub.Framework;

/// <summary>
///     Behavior that automatically focuses the first focusable control when the parent control is loaded.
///     Respects TabIndex ordering and only focuses visible, enabled controls.
/// </summary>
/// <param name="control">The parent control to search for focusable children.</param>
public class InitialFocusBehavior(Control control) : Behavior(control)
{
    /// <summary>
    ///     Called when the control is loaded. Finds and focuses the first focusable descendant.
    /// </summary>
    protected override void OnControlLoaded()
    {
        base.OnControlLoaded();

        var first = Control
            .GetVisualDescendants()
            .OfType<Control>()
            .Where(c => c is { Focusable: true, IsEffectivelyVisible: true, IsEffectivelyEnabled: true })
            .OrderBy(c => c.TabIndex) // OrderBy in LINQ is stable; ties keep tree order
            .FirstOrDefault();

        first?.Focus();
    }
}
