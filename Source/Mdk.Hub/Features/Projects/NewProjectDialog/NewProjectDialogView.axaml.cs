using Avalonia.Controls;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Projects.Overview;

namespace Mdk.Hub.Features.Projects.NewProjectDialog;

[Dependency]
public partial class NewProjectDialogView : UserControl
{
    public NewProjectDialogView()
    {
        InitializeComponent();
        
        Loaded += OnLoaded;
    }

    void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Focus the project name textbox
        var textBox = this.FindControl<TextBox>("ProjectNameTextBox");
        textBox?.Focus();
    }

    public static NewProjectDialogMessage DesignMessage => new()
    {
        Title = "Create New Project",
        Message = "Enter a name and location for your new project.",
        ProjectType = ProjectType.IngameScript,
        DefaultLocation = @"C:\Users\Example\Documents\Projects",
        OkText = "Create",
        CancelText = "Cancel"
    };
}
