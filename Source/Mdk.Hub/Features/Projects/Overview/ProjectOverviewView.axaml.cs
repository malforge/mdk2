using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using JetBrains.Annotations;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.Projects.Overview;

[Singleton]
[UsedImplicitly]
public partial class ProjectOverviewView : UserControl
{
    public ProjectOverviewView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ProjectOverviewViewModel viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            viewModel.ShowProjectRequested += OnShowProjectRequested;
        }
    }

    void OnShowProjectRequested(object? sender, ShowProjectEventArgs e) => ScrollToItem(e.Project);

    void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Scrolling now handled via ShowProjectRequested event
    }

    void ScrollToItem(ProjectModel item)
    {
        if (ProjectScrollViewer == null || ProjectItemsControl == null)
        {
            Debug.WriteLine("ScrollToItem: ScrollViewer or ItemsControl is null");
            return;
        }

        Debug.WriteLine($"ScrollToItem: Looking for item {item}");

        // Get the index of the item
        var items = ProjectItemsControl.ItemsSource as IList;
        if (items == null)
        {
            Debug.WriteLine("ScrollToItem: ItemsSource is not IList");
            return;
        }

        var index = items.IndexOf(item);
        if (index < 0)
        {
            Debug.WriteLine("ScrollToItem: Item not found in ItemsSource");
            return;
        }

        Debug.WriteLine($"ScrollToItem: Item is at index {index}");

        // Calculate approximate scroll position
        // Assuming each item is roughly the same height, estimate position
        const double estimatedItemHeight = 60.0; // Adjust based on actual item height
        var targetOffset = index * estimatedItemHeight;

        Debug.WriteLine($"ScrollToItem: Scrolling to offset {targetOffset}");
        ProjectScrollViewer.Offset = new Vector(0, targetOffset);
    }

    static T? FindVisualChild<T>(Control parent, Func<T, bool> predicate) where T : Control
    {
        foreach (var child in parent.GetVisualChildren())
        {
            if (child is T typedChild && predicate(typedChild))
                return typedChild;

            if (child is Control childControl)
            {
                var result = FindVisualChild(childControl, predicate);
                if (result != null)
                    return result;
            }
        }
        return null;
    }
}
