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
    readonly Action onExecuted;

    public SnackbarActionViewModel(SnackbarAction action, Action onExecuted)
    {
        _action = action;
        this.onExecuted = onExecuted;
        Command = new RelayCommand(Execute);
    }

    public string Text => _action.Text;
    public ICommand Command { get; }

    void Execute()
    {
        _action.Action(_action.Context);

        if (_action.IsClosingAction)
            onExecuted();
    }
}
