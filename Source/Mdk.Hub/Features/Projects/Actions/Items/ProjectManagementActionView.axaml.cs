using Avalonia.Controls;
using Avalonia.Input;
using Mal.DependencyInjection;

namespace Mdk.Hub.Features.Projects.Actions.Items;

[Instance]
public partial class ProjectManagementActionView : UserControl
{
    public ProjectManagementActionView()
    {
        InitializeComponent();
    }

    void OnCreateScriptTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is ProjectManagementAction action)
        {
            if (action.CreateScriptCommand.CanExecute(null))
                action.CreateScriptCommand.Execute(null);
        }
    }

    void OnCreateModTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is ProjectManagementAction action)
        {
            if (action.CreateModCommand.CanExecute(null))
                action.CreateModCommand.Execute(null);
        }
    }

    void OnAddExistingTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is ProjectManagementAction action)
        {
            if (action.AddExistingCommand.CanExecute(null))
                action.AddExistingCommand.Execute(null);
        }
    }
}
