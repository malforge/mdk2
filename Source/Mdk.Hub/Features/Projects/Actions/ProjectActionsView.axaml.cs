using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mal.DependencyInjection;

namespace Mdk.Hub.Features.Projects.Actions;

[Dependency]
public partial class ProjectActionsView : UserControl
{
    public ProjectActionsView()
    {
        InitializeComponent();
    }

    void OnAddExistingProjectTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.Tag is AddExistingProjectAction action)
        {
            if (action.AddCommand.CanExecute(null))
                action.AddCommand.Execute(null);
        }
    }
}
