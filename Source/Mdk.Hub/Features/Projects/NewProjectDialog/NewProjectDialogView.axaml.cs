using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Projects.Overview.Icons;
using Mdk.Hub.Features.Storage;

namespace Mdk.Hub.Features.Projects.NewProjectDialog;

/// <summary>
///     User control for creating new projects with name and location input.
/// </summary>
[Instance]
public partial class NewProjectDialogView : UserControl
{
    /// <summary>
    ///     Initializes a new instance of the NewProjectDialogView class.
    /// </summary>
    public NewProjectDialogView()
    {
        InitializeComponent();

        Loaded += OnLoaded;
    }

    /// <summary>
    ///     Gets design-time sample data for visual preview.
    /// </summary>
    public static NewProjectDialogMessage DesignMessage
    {
        get
        {
            var fileStorage = new FileStorageService();
            return new()
            {
                Title = "Create New Project",
                Message = "Enter a name and location for your new project.",
                Icon = new ProgrammableBlockSymbol(),
                DefaultProjectName = "MdkScriptProject",
                DefaultLocation = Path.Combine(fileStorage.GetDocumentsPath(), "Projects"),
                OkText = "Create",
                CancelText = "Cancel"
            };
        }
    }

    void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Focus the project name textbox
        var textBox = this.FindControl<TextBox>("ProjectNameTextBox");
        textBox?.Focus();
    }
}
