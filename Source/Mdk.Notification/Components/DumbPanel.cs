using System;
using Avalonia;
using Avalonia.Controls;

namespace Mdk.Notification.Components;

public class DumbPanel : Panel
{
    public Size ContentSize { get; private set; }
    
    protected override Size MeasureOverride(Size availableSize)
    {
        var contentSize = new Size();

        foreach (var child in Children)
        {
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            contentSize = new Size(Math.Max(contentSize.Width, child.DesiredSize.Width), Math.Max(contentSize.Height, child.DesiredSize.Height));
        }

        ContentSize = contentSize;
        return new Size(contentSize.Width, contentSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
            child.Arrange(new Rect(new Point(0, 0), child.DesiredSize));

        return finalSize;
    }
}