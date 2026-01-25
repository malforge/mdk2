using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Mdk.Hub.Framework;

/// <summary>
/// Base class for Avalonia control behaviors that respond to control lifecycle events.
/// Automatically handles subscription and unsubscription to control Loaded/Unloaded events.
/// </summary>
public abstract class Behavior : IDisposable
{
    Control? _control;

    /// <summary>
    /// Initializes a new instance of the <see cref="Behavior"/> class.
    /// </summary>
    /// <param name="control">The control to attach the behavior to.</param>
    protected Behavior(Control control)
    {
        _control = control;
        Control.Loaded += OnControlLoaded;
        Control.Unloaded += OnControlUnloaded;
    }

    /// <summary>
    /// Gets the control this behavior is attached to.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the behavior has been disposed.</exception>
    protected Control Control => _control ?? throw new ObjectDisposedException(nameof(Behavior));

    /// <summary>
    /// Disposes the behavior and unsubscribes from control events.
    /// </summary>
    public void Dispose()
    {
        if (_control is null)
            return;

        if (_control.IsLoaded)
            OnControlUnloaded();

        _control.Loaded -= OnControlLoaded;
        _control.Unloaded -= OnControlUnloaded;
        _control = null;
    }

    void OnControlUnloaded(object? sender, RoutedEventArgs e) => OnControlUnloaded();

    /// <summary>
    /// Called when the control is unloaded from the visual tree.
    /// Override to perform cleanup or unsubscribe from events.
    /// </summary>
    protected virtual void OnControlUnloaded() { }

    void OnControlLoaded(object? sender, RoutedEventArgs e) => OnControlLoaded();

    /// <summary>
    /// Called when the control is loaded into the visual tree.
    /// Override to perform initialization or subscribe to events.
    /// </summary>
    protected virtual void OnControlLoaded() { }
}