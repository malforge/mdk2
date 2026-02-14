using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Mdk.Hub.Features.Projects.Actions;

/// <summary>
///     Describes the visual edge characteristics of an action item for spacing calculations.
/// </summary>
[Flags]
public enum ActionEdgeType
{
    /// <summary>
    ///     The edge is bare - no visual container, needs less spacing.
    /// </summary>
    Bare = 0,
    
    /// <summary>
    ///     The edge is contained - has a visual container (card, padding), needs more spacing when adjacent to another contained edge.
    /// </summary>
    Contained = 1
}

/// <summary>
///     Base control for action item views. Replaces UserControl with edge metadata for intelligent spacing.
/// </summary>
public class ActionItemControl : UserControl
{
    /// <summary>
    ///     Identifies the <see cref="TopEdge"/> styled property.
    /// </summary>
    public static readonly StyledProperty<ActionEdgeType> TopEdgeProperty =
        AvaloniaProperty.Register<ActionItemControl, ActionEdgeType>(
            nameof(TopEdge), 
            ActionEdgeType.Bare);

    /// <summary>
    ///     Identifies the <see cref="BottomEdge"/> styled property.
    /// </summary>
    public static readonly StyledProperty<ActionEdgeType> BottomEdgeProperty =
        AvaloniaProperty.Register<ActionItemControl, ActionEdgeType>(
            nameof(BottomEdge), 
            ActionEdgeType.Bare);

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

/// <summary>
///     Custom panel that arranges action items vertically with intelligent spacing.
///     Spacing adapts based on edge characteristics: when two compact edges meet, minimal spacing is used.
/// </summary>
public class ActionItemPanel : Panel
{
    /// <summary>
    ///     Identifies the <see cref="BareSpacing"/> styled property.
    /// </summary>
    public static readonly StyledProperty<double> BareSpacingProperty =
        AvaloniaProperty.Register<ActionItemPanel, double>(nameof(BareSpacing), 8.0);

    /// <summary>
    ///     Identifies the <see cref="ContainedSpacing"/> styled property.
    /// </summary>
    public static readonly StyledProperty<double> ContainedSpacingProperty =
        AvaloniaProperty.Register<ActionItemPanel, double>(nameof(ContainedSpacing), 16.0);

    /// <summary>
    ///     Spacing to use when at least one adjacent edge is bare.
    ///     Smaller value since bare edges have intrinsic visual space.
    /// </summary>
    public double BareSpacing
    {
        get => GetValue(BareSpacingProperty);
        set => SetValue(BareSpacingProperty, value);
    }

    /// <summary>
    ///     Spacing to use when both adjacent edges are contained.
    ///     Larger value to provide breathing room between visually dense items.
    /// </summary>
    public double ContainedSpacing
    {
        get => GetValue(ContainedSpacingProperty);
        set => SetValue(ContainedSpacingProperty, value);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0.0;
        var height = 0.0;

        foreach (var child in Children)
        {
            child.Measure(availableSize);
            var desiredSize = child.DesiredSize;
            width = Math.Max(width, desiredSize.Width);
            height += desiredSize.Height;
        }

        // Add spacing between items
        for (var i = 0; i < Children.Count - 1; i++)
        {
            var currentBottomEdge = GetBottomEdge(Children[i]);
            var nextTopEdge = GetTopEdge(Children[i + 1]);
            height += (currentBottomEdge == ActionEdgeType.Contained && nextTopEdge == ActionEdgeType.Contained)
                ? ContainedSpacing
                : BareSpacing;
        }

        return new Size(width, height);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var y = 0.0;

        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var childHeight = child.DesiredSize.Height;

            child.Arrange(new Rect(0, y, finalSize.Width, childHeight));
            y += childHeight;

            // Add spacing after this child (if not the last one)
            if (i < Children.Count - 1)
            {
                var currentBottomEdge = GetBottomEdge(child);
                var nextTopEdge = GetTopEdge(Children[i + 1]);
                y += (currentBottomEdge == ActionEdgeType.Contained && nextTopEdge == ActionEdgeType.Contained)
                    ? ContainedSpacing
                    : BareSpacing;
            }
        }

        return finalSize;
    }

    static ActionEdgeType GetTopEdge(Control control)
    {
        var actionItem = FindActionItemControl(control);
        return actionItem?.TopEdge ?? ActionEdgeType.Bare;
    }

    static ActionEdgeType GetBottomEdge(Control control)
    {
        var actionItem = FindActionItemControl(control);
        return actionItem?.BottomEdge ?? ActionEdgeType.Bare;
    }

    static ActionItemControl? FindActionItemControl(Control control)
    {
        // Check if it's directly an ActionItemControl
        if (control is ActionItemControl actionItem)
            return actionItem;
        
        // Recursively search visual children
        foreach (var child in control.GetVisualChildren())
        {
            if (child is Control childControl)
            {
                var found = FindActionItemControl(childControl);
                if (found != null)
                    return found;
            }
        }
        
        return null;
    }
}

