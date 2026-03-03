using System;
using System.Windows.Input;

namespace Mdk.Hub.Framework;

/// <summary>
///     A simple implementation of <see cref="ICommand" /> that relays its execution to a delegate.
/// </summary>
public class RelayCommand : ICommand
{
    readonly Func<object?, bool>? _canExecute;
    readonly Action<object?> _execute;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RelayCommand" /> class.
    /// </summary>
    /// <param name="execute">The action to execute.</param>
    /// <param name="canExecute">Optional function to determine if the command can execute.</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute != null ? _ => canExecute() : null) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RelayCommand" /> class.
    /// </summary>
    /// <param name="execute">The action to execute with a parameter.</param>
    /// <param name="canExecute">Optional function to determine if the command can execute.</param>
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    ///     Occurs when changes occur that affect whether or not the command should execute.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    ///     Determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">Data used by the command.</param>
    /// <returns><c>true</c> if this command can be executed; otherwise, <c>false</c>.</returns>
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    /// <summary>
    ///     Executes the command.
    /// </summary>
    /// <param name="parameter">Data used by the command.</param>
    public void Execute(object? parameter) => _execute(parameter);

    /// <summary>
    ///     Raises the <see cref="CanExecuteChanged" /> event.
    /// </summary>
    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
///     A simple implementation of <see cref="ICommand" /> that relays its execution to a delegate with generic parameter
///     support.
/// </summary>
/// <typeparam name="T">The type of the command parameter.</typeparam>
public class RelayCommand<T> : ICommand
{
    readonly Func<T?, bool>? _canExecute;
    readonly Action<T?> _execute;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RelayCommand{T}" /> class.
    /// </summary>
    /// <param name="execute">The action to execute with a typed parameter.</param>
    /// <param name="canExecute">Optional function to determine if the command can execute.</param>
    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    ///     Occurs when changes occur that affect whether or not the command should execute.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    ///     Determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">Data used by the command.</param>
    /// <returns><c>true</c> if this command can be executed; otherwise, <c>false</c>.</returns>
    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

    /// <summary>
    ///     Executes the command.
    /// </summary>
    /// <param name="parameter">Data used by the command.</param>
    public void Execute(object? parameter) => _execute((T?)parameter);

    /// <summary>
    ///     Raises the <see cref="CanExecuteChanged" /> event.
    /// </summary>
    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}