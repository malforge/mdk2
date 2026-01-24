using System;

namespace Mdk.Hub.Features.Shell;

public readonly struct UnsavedChangesHandle : IDisposable
{
    readonly Action _disposeAction;
    
    internal UnsavedChangesHandle(Action disposeAction)
    {
        _disposeAction = disposeAction;
    }
    
    public void Dispose()
    {
        _disposeAction?.Invoke();
    }
}

public readonly struct UnsavedChangesInfo
{
    public string Description { get; init; }
    public Action GoThereAction { get; init; }
}
