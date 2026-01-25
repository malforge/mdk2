using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.CommonDialogs;

[ViewModelFor<BusyOverlayView>]
public class BusyOverlayViewModel : OverlayModel
{
    string _message;
    double _progress;
    bool _isIndeterminate = true;

    public BusyOverlayViewModel(string message)
    {
        _message = message;
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
}
