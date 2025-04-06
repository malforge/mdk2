using System.ComponentModel;
using System.Runtime.InteropServices.ComTypes;
using Avalonia.Controls;

namespace Mdk.Notification.ViewModels;

public class ToastWindowViewModel : ViewModelBase
{
    Toast? _toast;

    public ToastWindowViewModel()
    {
#if DEBUG
        if (Design.IsDesignMode)
            _toast = new MessageToast("Hello, World!", [new ToastAction("OK", _ => { })]);
#endif
    }

    public ToastWindowViewModel(Toast toast)
    {
        _toast = toast;
    }

    public Toast? Toast
    {
        get => _toast;
        set => SetProperty(ref _toast, value);
    }
}