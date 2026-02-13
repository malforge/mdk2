using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    Stopwatch? _timeoutStopwatch;
    int _remainingTimeout;
    bool _isPaused;

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
    /// Gets or sets whether this snackbar is in toast mode (no close button, short-lived).
    /// </summary>
    public bool IsToast { get; set; }

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
            _remainingTimeout = Timeout;
            _timeoutStopwatch = Stopwatch.StartNew();
            _timeoutCts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_remainingTimeout, _timeoutCts.Token);
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
    /// Pauses the timeout (when mouse hovers over interactive snackbar).
    /// </summary>
    public void PauseTimeout()
    {
        if (_timeoutCts != null && !_isPaused && !IsToast)
        {
            _isPaused = true;
            _timeoutStopwatch?.Stop();
            _remainingTimeout -= (int)(_timeoutStopwatch?.ElapsedMilliseconds ?? 0);
            CancelTimeout();
        }
    }

    /// <summary>
    /// Resumes the timeout (when mouse leaves interactive snackbar).
    /// </summary>
    public void ResumeTimeout()
    {
        if (_isPaused && !IsToast && _remainingTimeout > 0)
        {
            _isPaused = false;
            _timeoutStopwatch = Stopwatch.StartNew();
            _timeoutCts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_remainingTimeout, _timeoutCts.Token);
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
