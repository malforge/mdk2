using System;
using Avalonia.Controls;

namespace Mdk.Hub.Features.Input;

/// <summary>
///     Manages a stack of keyboard shortcut scopes per top-level window.
///     Only the topmost scope on each window receives key events.
///     Dispose the returned handle to pop the scope.
/// </summary>
public interface IKeyScopeService
{
    /// <summary>
    ///     Pushes a new scope onto the stack for the given top-level window.
    ///     Dispose the returned handle to pop the scope.
    /// </summary>
    /// <param name="topLevel">The window this scope is attached to.</param>
    /// <param name="bindings">The key bindings active in this scope.</param>
    /// <returns>A disposable that removes this scope when disposed.</returns>
    IDisposable PushScope(TopLevel topLevel, params KeyScopeBinding[] bindings);
}
