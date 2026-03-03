using Avalonia;
using Avalonia.Controls;

namespace Mdk.Hub.Features.Projects.Actions;

/// <summary>
///     Base control for action item views. Replaces UserControl with edge metadata for intelligent spacing.
/// </summary>
public class ActionItemControl : UserControl
{
    /// <summary>
    ///     Identifies the <see cref="TopEdge" /> styled property.
    /// </summary>
    public static readonly StyledProperty<ActionEdgeType> TopEdgeProperty =
        AvaloniaProperty.Register<ActionItemControl, ActionEdgeType>(
            nameof(TopEdge));

    /// <summary>
    ///     Identifies the <see cref="BottomEdge" /> styled property.
    /// </summary>
    public static readonly StyledProperty<ActionEdgeType> BottomEdgeProperty =
        AvaloniaProperty.Register<ActionItemControl, ActionEdgeType>(
            nameof(BottomEdge));

    static ActionItemControl()
    {
        TopEdgeProperty.Changed.AddClassHandler<ActionItemControl>((control, args) =>
        {
            if (control.Parent is ActionItemPanel panel)
                panel.InvalidateMeasure();
        });

        BottomEdgeProperty.Changed.AddClassHandler<ActionItemControl>((control, args) =>
        {
            if (control.Parent is ActionItemPanel panel)
                panel.InvalidateMeasure();
        });
    }

    /// <summary>
    ///     The visual edge type of this action's top edge.
    /// </summary>
    public ActionEdgeType TopEdge
    {
        get => GetValue(TopEdgeProperty);
        set => SetValue(TopEdgeProperty, value);
    }

    /// <summary>
    ///     The visual edge type of this action's bottom edge.
    /// </summary>
    public ActionEdgeType BottomEdge
    {
        get => GetValue(BottomEdgeProperty);
        set => SetValue(BottomEdgeProperty, value);
    }
}