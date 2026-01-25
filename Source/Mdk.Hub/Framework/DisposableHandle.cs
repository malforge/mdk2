using System;

namespace Mdk.Hub.Framework;

/// <summary>
/// Represents a disposable handle that executes a specified action when disposed.
/// Useful for managing temporary subscriptions or registrations.
/// </summary>
/// <param name="disposeAction">The action to execute on disposal.</param>
public class DisposableHandle(Action disposeAction) : IDisposable
{
    Action? _disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));

    /// <summary>
    /// Executes the dispose action and ensures it's only called once.
    /// </summary>
    public void Dispose()
    {
        _disposeAction?.Invoke();
        _disposeAction = null;
    }
}