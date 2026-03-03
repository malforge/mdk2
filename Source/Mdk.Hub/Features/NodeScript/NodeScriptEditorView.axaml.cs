using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.NodeScript;

/// <summary>
///     View for the node-based script editor.
/// </summary>
[Instance]
public partial class NodeScriptEditorView : UserControl
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="NodeScriptEditorView"/> class.
    /// </summary>
    public NodeScriptEditorView()
    {
        InitializeComponent();
        OverlayPanel.GetObservable(IsVisibleProperty).Subscribe(isVisible =>
        {
            if (isVisible)
                OverlayPanel.Focus();
        });
    }

    void OnNodeContentPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(sender as Control);
        
        // Check for double-click (ClickCount == 2) with left button
        if (point.Properties.IsLeftButtonPressed && e.ClickCount == 2)
        {
            
            if (sender is Control { DataContext: not null } control)
            {
                if (DataContext is NodeScriptEditorViewModel viewModel)
                {
                    viewModel.OpenNodeEditor(control.DataContext);
                    e.Handled = true;
                }
            }
        }
    }

    void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        
        // Check if right button was pressed
        if (point.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
        {
            if (DataContext is NodeScriptEditorViewModel viewModel)
            {
                var position = e.GetPosition(this);
                viewModel.OpenAddNodeMenu(position);
                e.Handled = true;
            }
        }
    }

    void OnNodeContentDoubleTapped(object? sender, TappedEventArgs e)
    {
        
    }

    void OnNodeDoubleTapped(object? sender, TappedEventArgs e)
    {
        throw new System.NotImplementedException();
    }
}
