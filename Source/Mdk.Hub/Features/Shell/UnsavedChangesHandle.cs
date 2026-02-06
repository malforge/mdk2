using System;

namespace Mdk.Hub.Features.Shell;

/// <summary>
///     A handle that represents unsaved changes. Disposing this handle indicates the changes have been saved or discarded.
/// </summary>
public readonly struct UnsavedChangesHandle : IDisposable
{
    readonly Action _disposeAction;

    internal UnsavedChangesHandle(Action disposeAction)
    {
        _disposeAction = disposeAction;
    }

    /// <summary>
    ///     Releases the unsaved changes handle.
    /// </summary>
    public void Dispose() => _disposeAction?.Invoke();
}