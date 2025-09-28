using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Mdk.Hub.Framework;

public abstract class Behavior : IDisposable
{
    Control? _control;

    protected Behavior(Control control)
    {
        _control = control;
        Control.Loaded += OnControlLoaded;
        Control.Unloaded += OnControlUnloaded;
    }

    protected Control Control => _control ?? throw new ObjectDisposedException(nameof(Behavior));

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

    protected virtual void OnControlUnloaded() { }

    void OnControlLoaded(object? sender, RoutedEventArgs e) => OnControlLoaded();

    protected virtual void OnControlLoaded() { }
}