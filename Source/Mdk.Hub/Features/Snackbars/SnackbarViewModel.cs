using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Snackbars;

/// <summary>
///     View model for a snackbar notification window.
/// </summary>
[ViewModelFor<SnackbarWindow>]
public class SnackbarViewModel : ViewModel
{
    readonly RelayCommand _closeCommand;
    CancellationTokenSource? _timeoutCts;

    /// <summary>
    /// Initializes a new instance of the <see cref="SnackbarViewModel"/> class.
    /// </summary>
    public SnackbarViewModel()
    {
        _closeCommand = new RelayCommand(Close);
    }

    /// <summary>
    /// Gets or sets the message text to display in the snackbar.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the collection of actions available in the snackbar.
    /// </summary>
    public IReadOnlyList<SnackbarActionViewModel> Actions { get; set; } = Array.Empty<SnackbarActionViewModel>();
    /// <summary>
    /// Gets or sets the timeout duration in milliseconds before the snackbar auto-closes.
    /// </summary>
    public int Timeout { get; set; }

    /// <summary>
    /// Gets the command to close the snackbar.
    /// </summary>
    public ICommand CloseCommand => _closeCommand;

    /// <summary>
    /// Event raised when the snackbar should be closed.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Sets the available actions for the snackbar.
    /// </summary>
    public void SetActions(IEnumerable<SnackbarAction> actions) => Actions = actions.Select(a => new SnackbarActionViewModel(a, CloseIfRequested)).ToList();

    void CloseIfRequested()
    {
        CancelTimeout();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Starts the auto-close timeout if a timeout is configured.
    /// </summary>
    public void StartTimeout()
    {
        if (Timeout > 0)
        {
            _timeoutCts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(Timeout, _timeoutCts.Token);
                    if (!_timeoutCts.Token.IsCancellationRequested)
                        CloseRequested?.Invoke(this, EventArgs.Empty);
                }
                catch (TaskCanceledException)
                {
                    // Timeout was cancelled, ignore
                }
            });
        }
    }

    /// <summary>
    /// Cancels the auto-close timeout.
    /// </summary>
    public void CancelTimeout()
    {
        _timeoutCts?.Cancel();
        _timeoutCts?.Dispose();
        _timeoutCts = null;
    }

    void Close() => CloseIfRequested();
}
