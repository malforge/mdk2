using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Mdk.Hub.Features.Diagnostics;

namespace Mdk.Hub.Framework;

/// <summary>
///     An asynchronous implementation of <see cref="ICommand" /> that prevents concurrent execution.
///     Exceptions during execution are logged but do not crash the application.
/// </summary>
public class AsyncRelayCommand : ICommand
{
    readonly Func<bool>? _canExecute;
    readonly Func<Task> _execute;
    readonly ILogger? _logger;
    bool _isExecuting;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncRelayCommand" /> class.
    /// </summary>
    /// <param name="execute">The asynchronous action to execute.</param>
    /// <param name="canExecute">Optional function to determine if the command can execute.</param>
    /// <param name="logger">Optional logger for exception reporting.</param>
    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null, ILogger? logger = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        _logger = logger;
    }

    /// <summary>
    ///     Occurs when changes occur that affect whether or not the command should execute.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    ///     Determines whether the command can execute in its current state.
    ///     Returns <c>false</c> while the command is already executing.
    /// </summary>
    /// <param name="parameter">Data used by the command.</param>
    /// <returns><c>true</c> if this command can be executed; otherwise, <c>false</c>.</returns>
    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

    /// <summary>
    ///     Executes the command asynchronously. Exceptions are caught, logged, and re-thrown to the global exception handler.
    /// </summary>
    /// <param name="parameter">Data used by the command.</param>
    public async void Execute(object? parameter)
    {
        try
        {
            if (!CanExecute(parameter))
                return;

            _isExecuting = true;
            NotifyCanExecuteChanged();

            await _execute();
        }
        catch (Exception ex)
        {
            _logger?.Error("Exception in async command execution", ex);
            ExecutionFailed?.Invoke(this, ex);
        }
        finally
        {
            _isExecuting = false;
            NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    ///     Occurs when an exception is thrown during command execution.
    /// </summary>
    public event EventHandler<Exception>? ExecutionFailed;

    /// <summary>
    ///     Raises the <see cref="CanExecuteChanged" /> event.
    /// </summary>
    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
///     An asynchronous implementation of <see cref="ICommand" /> with generic parameter support that prevents concurrent
///     execution.
///     Exceptions during execution are logged but do not crash the application.
/// </summary>
/// <typeparam name="T">The type of the command parameter.</typeparam>
public class AsyncRelayCommand<T> : ICommand
{
    readonly Func<T?, bool>? _canExecute;
    readonly Func<T?, Task> _execute;
    readonly ILogger? _logger;
    bool _isExecuting;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncRelayCommand{T}" /> class.
    /// </summary>
    /// <param name="execute">The asynchronous action to execute with a typed parameter.</param>
    /// <param name="canExecute">Optional function to determine if the command can execute.</param>
    /// <param name="logger">Optional logger for exception reporting.</param>
    public AsyncRelayCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null, ILogger? logger = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        _logger = logger;
    }

    /// <summary>
    ///     Occurs when changes occur that affect whether or not the command should execute.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    ///     Determines whether the command can execute in its current state.
    ///     Returns <c>false</c> while the command is already executing.
    /// </summary>
    /// <param name="parameter">Data used by the command.</param>
    /// <returns><c>true</c> if this command can be executed; otherwise, <c>false</c>.</returns>
    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke((T?)parameter) ?? true);

    /// <summary>
    ///     Executes the command asynchronously. Exceptions are caught, logged, and re-thrown to the global exception handler.
    /// </summary>
    /// <param name="parameter">Data used by the command.</param>
    public async void Execute(object? parameter)
    {
        try
        {
            if (!CanExecute(parameter))
                return;

            _isExecuting = true;
            NotifyCanExecuteChanged();

            await _execute((T?)parameter);
        }
        catch (Exception ex)
        {
            _logger?.Error("Exception in async command execution", ex);
            ExecutionFailed?.Invoke(this, ex);
        }
        finally
        {
            _isExecuting = false;
            NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    ///     Occurs when an exception is thrown during command execution.
    /// </summary>
    public event EventHandler<Exception>? ExecutionFailed;

    /// <summary>
    ///     Raises the <see cref="CanExecuteChanged" /> event.
    /// </summary>
    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}