using System.Windows.Input;

namespace Mdk.Notification.Windows;

public class ModelCommand(Action action, bool isEnabled = true) : ICommand
{
    readonly Action _action = action;
    bool _isEnabled = isEnabled;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value)
                return;
            _isEnabled = value;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    bool ICommand.CanExecute(object? parameter) => IsEnabled;

    void ICommand.Execute(object? parameter) => Execute();

    public event EventHandler? CanExecuteChanged;

    public void Execute() => _action();
}