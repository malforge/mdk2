using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Projects.Overview;

namespace Mdk.Hub.Features.Projects.NewProjectDialog;

[Instance]
public partial class NewProjectDialogView : UserControl
{
    public NewProjectDialogView()
    {
        InitializeComponent();

        Loaded += OnLoaded;
    }

    public static NewProjectDialogMessage DesignMessage => new()
    {
        Title = "Create New Project",
        Message = "Enter a name and location for your new project.",
        ProjectType = ProjectType.ProgrammableBlock,
        DefaultLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Projects"),
        OkText = "Create",
        CancelText = "Cancel"
    };

    void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Focus the project name textbox
        var textBox = this.FindControl<TextBox>("ProjectNameTextBox");
        textBox?.Focus();
    }
}
