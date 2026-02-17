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
    }

    void OnTestMenuClick(object? sender, RoutedEventArgs e)
    {
        // Test button to verify menu system works
        if (DataContext is NodeScriptEditorViewModel viewModel)
        {
            viewModel.OpenAddNodeMenu(new Point(200, 200));
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
}
