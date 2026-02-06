using System.Threading;
using System.Windows.Input;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.CommonDialogs;

/// <summary>
/// View model for a busy/loading overlay with progress indication.
/// </summary>
[ViewModelFor<BusyOverlayView>]
public class BusyOverlayViewModel : OverlayModel
{
    CancellationTokenSource? _cancellationTokenSource;
    bool _isCancellable;
    bool _isIndeterminate = true;
    string _message;
    double _progress;

    /// <summary>
    /// Initializes a new instance of the <see cref="BusyOverlayViewModel"/> class.
    /// </summary>
    public BusyOverlayViewModel(string message)
    {
        _message = message;
        CancelCommand = new RelayCommand(Cancel);
    }

    /// <summary>
    /// Gets or sets the message text to display in the busy overlay.
    /// </summary>
    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    /// <summary>
    /// Gets or sets the progress value (0.0 to 1.0) for determinate progress indication.
    /// </summary>
    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    /// <summary>
    /// Gets or sets whether the progress is indeterminate (no specific progress value).
    /// </summary>
    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        set => SetProperty(ref _isIndeterminate, value);
    }

    /// <summary>
    /// Gets or sets whether the operation can be cancelled by the user.
    /// </summary>
    public bool IsCancellable
    {
        get => _isCancellable;
        set => SetProperty(ref _isCancellable, value);
    }

    /// <summary>
    /// Gets the command to cancel the operation.
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// Gets the cancellation token for the operation.
    /// </summary>
    public CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? CancellationToken.None;

    /// <summary>
    /// Enables cancellation support for this overlay.
    /// </summary>
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
