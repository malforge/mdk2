using System;
using Avalonia.Input;

namespace Mdk.Hub.Features.Input;

/// <summary>
///     A key gesture paired with an action, used to define a scoped keyboard shortcut.
/// </summary>
/// <param name="Key">The key that triggers the binding.</param>
/// <param name="Modifiers">The required modifier keys.</param>
/// <param name="Handler">The action to invoke when the gesture is matched.</param>
public readonly record struct KeyScopeBinding(Key Key, KeyModifiers Modifiers, Action Handler);
