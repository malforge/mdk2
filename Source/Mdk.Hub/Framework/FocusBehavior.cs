using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Mdk.Hub.Framework;

public class InitialFocusBehavior(Control control) : Behavior(control)
{
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