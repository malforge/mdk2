using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.Settings;

/// <summary>
///     View for configuring global application settings.
/// </summary>
[Singleton]
public partial class GlobalSettingsView : UserControl
{
    /// <summary>
    ///     Initializes a new instance of the GlobalSettingsView class.
    /// </summary>
    public GlobalSettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is GlobalSettingsViewModel viewModel)
        {
            viewModel.FocusFirstInvalidField += OnFocusFirstInvalidField;
        }
    }

    void OnFocusFirstInvalidField(object? sender, EventArgs e)
    {
        // Find the first FolderBrowserControl with HasError=true
        var invalidControls = this.GetLogicalDescendants()
            .OfType<Controls.FolderBrowserControl>()
            .Where(c => c.HasError)
            .ToList();

        if (invalidControls.Count > 0)
        {
            var firstInvalid = invalidControls[0];
            
            // Scroll into view
            firstInvalid.BringIntoView();
            
            // Focus the textbox inside
            var textBox = firstInvalid.GetLogicalDescendants().OfType<TextBox>().FirstOrDefault();
            textBox?.Focus();
        }
    }
}
