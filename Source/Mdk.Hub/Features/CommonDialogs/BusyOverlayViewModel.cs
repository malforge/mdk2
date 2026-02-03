using System.Threading;
using System.Windows.Input;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.CommonDialogs;

[ViewModelFor<BusyOverlayView>]
public class BusyOverlayViewModel : OverlayModel
{
    CancellationTokenSource? _cancellationTokenSource;
    bool _isCancellable;
    bool _isIndeterminate = true;
    string _message;
    double _progress;

    public BusyOverlayViewModel(string message)
    {
        _message = message;
        CancelCommand = new RelayCommand(Cancel);
    }

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        set => SetProperty(ref _isIndeterminate, value);
    }

    public bool IsCancellable
    {
        get => _isCancellable;
        set => SetProperty(ref _isCancellable, value);
    }

    public ICommand CancelCommand { get; }

    public CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? CancellationToken.None;

    public void EnableCancellation()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        IsCancellable = true;
    }

    void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        IsCancellable = false;
    }
}
