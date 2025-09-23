using System;

namespace Mdk.Hub.Framework;

public class DisposableHandle(Action disposeAction) : IDisposable
{
    Action? _disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));

    public void Dispose()
    {
        _disposeAction?.Invoke();
        _disposeAction = null;
    }
}