using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Snackbars;

/// <summary>
/// View model for a snackbar notification window.
/// </summary>
[ViewModelFor<SnackbarWindow>]
public partial class SnackbarViewModel : ViewModel
{
    CancellationTokenSource? _timeoutCts;
    
    public string Message { get; set; } = string.Empty;
    public IReadOnlyList<SnackbarActionViewModel> Actions { get; set; } = Array.Empty<SnackbarActionViewModel>();
    public int Timeout { get; set; }
    
    public event EventHandler? CloseRequested;

    public void SetActions(IEnumerable<SnackbarAction> actions)
    {
        Actions = actions.Select(a => new SnackbarActionViewModel(a, CloseIfRequested)).ToList();
    }

    void CloseIfRequested()
    {
        CancelTimeout();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
    
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
    
    public void CancelTimeout()
    {
        _timeoutCts?.Cancel();
        _timeoutCts?.Dispose();
        _timeoutCts = null;
    }
    
    [RelayCommand]
    void Close()
    {
        CloseIfRequested();
    }
}
