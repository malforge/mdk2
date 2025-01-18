using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Mdk.Hub.Utility;

namespace Mdk.Hub.ViewModels;

public class ViewModelBase : ObservableObject, INotifyDisposing, IDisposable
{
    bool _isDisposed;

    public void Dispose()
    {
        if (_isDisposed) return;
        GC.SuppressFinalize(this);
        _isDisposed = true;
        OnDisposing(true);
    }

    public event EventHandler? Disposing;

    ~ViewModelBase()
    {
        OnDisposing(false);
    }

    protected virtual void OnDisposing(bool disposing)
    {
        Disposing?.Invoke(this, EventArgs.Empty);
    }
}