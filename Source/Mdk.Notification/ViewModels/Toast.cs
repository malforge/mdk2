using System.ComponentModel;

namespace Mdk.Notification.ViewModels;

public abstract class Toast
{
    public event CancelEventHandler? Dismissed;

    public virtual bool Dismiss()
    {
        var cancelEventArgs = new CancelEventArgs();
        Dismissed?.Invoke(this, cancelEventArgs);
        return !cancelEventArgs.Cancel;
    }
}