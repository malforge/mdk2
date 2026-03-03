using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Mdk.Hub.Features.Projects.Actions;

/// <summary>
///     Custom panel that arranges action items vertically with intelligent spacing.
///     Spacing adapts based on edge characteristics: when two compact edges meet, minimal spacing is used.
/// </summary>
public class ActionItemPanel : Panel
{
    /// <summary>
    ///     Identifies the <see cref="BareSpacing" /> styled property.
    /// </summary>
    public static readonly StyledProperty<double> BareSpacingProperty =
        AvaloniaProperty.Register<ActionItemPanel, double>(nameof(BareSpacing), 8.0);

    /// <summary>
    ///     Identifies the <see cref="ContainedSpacing" /> styled property.
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
            height += currentBottomEdge == ActionEdgeType.Contained && nextTopEdge == ActionEdgeType.Contained
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
                y += currentBottomEdge == ActionEdgeType.Contained && nextTopEdge == ActionEdgeType.Contained
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