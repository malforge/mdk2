using Avalonia.Controls;
using Avalonia.Input;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.Projects.Actions.Items;

/// <summary>
///     View for project management actions (create, add existing).
/// </summary>
[Instance]
public partial class ProjectManagementActionView : UserControl
{
    /// <summary>
    ///     Initializes a new instance of the ProjectManagementActionView class.
    /// </summary>
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
