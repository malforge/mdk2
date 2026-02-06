using System;
using System.Windows.Input;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Snackbars;

/// <summary>
///     View model for a snackbar action button.
/// </summary>
public partial class SnackbarActionViewModel : ViewModel
{
    readonly SnackbarAction _action;
    readonly Action _onExecuted;

    /// <summary>
    ///     Initializes a new instance of the SnackbarActionViewModel class.
    /// </summary>
    /// <param name="action">The snackbar action to represent.</param>
    /// <param name="onExecuted">Callback invoked when the action is executed and should close the snackbar.</param>
    public SnackbarActionViewModel(SnackbarAction action, Action onExecuted)
    {
        _action = action;
        _onExecuted = onExecuted;
        Command = new RelayCommand(Execute);
    }

    /// <summary>
    ///     Gets the display text for the action button.
    /// </summary>
    public string Text => _action.Text;
    
    /// <summary>
    ///     Gets the command to execute when the action button is clicked.
    /// </summary>
    public ICommand Command { get; }

    void Execute()
    {
        _action.Action(_action.Context);

        if (_action.IsClosingAction)
            _onExecuted();
    }
}