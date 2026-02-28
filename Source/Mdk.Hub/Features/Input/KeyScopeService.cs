using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.Input;

/// <inheritdoc />
[Singleton<IKeyScopeService>]
public class KeyScopeService : IKeyScopeService
{
    readonly Dictionary<TopLevel, TopLevelEntry> _entries = new();

    /// <inheritdoc />
    public IDisposable PushScope(TopLevel topLevel, params KeyScopeBinding[] bindings)
    {
        if (!_entries.TryGetValue(topLevel, out var entry))
        {
            entry = new TopLevelEntry(topLevel, this);
            _entries[topLevel] = entry;
        }

        entry.Stack.Add(bindings);
        return new ScopeHandle(() => PopScope(topLevel, bindings));
    }

    void PopScope(TopLevel topLevel, KeyScopeBinding[] bindings)
    {
        if (!_entries.TryGetValue(topLevel, out var entry))
            return;

        entry.Stack.Remove(bindings);

        if (entry.Stack.Count == 0)
        {
            entry.Detach();
            _entries.Remove(topLevel);
        }
    }

    void OnKeyDown(TopLevel topLevel, KeyEventArgs e)
    {
        if (!_entries.TryGetValue(topLevel, out var entry) || entry.Stack.Count == 0)
            return;

        foreach (var binding in entry.Stack[^1])
        {
            if (e.Key == binding.Key && e.KeyModifiers == binding.Modifiers)
            {
                binding.Handler();
                e.Handled = true;
                return;
            }
        }
    }

    sealed class TopLevelEntry
    {
        readonly TopLevel _topLevel;
        readonly KeyScopeService _owner;
        readonly EventHandler<KeyEventArgs> _handler;

        public TopLevelEntry(TopLevel topLevel, KeyScopeService owner)
        {
            _topLevel = topLevel;
            _owner = owner;
            Stack = [];
            _handler = (_, e) => owner.OnKeyDown(topLevel, e);
            topLevel.AddHandler(InputElement.KeyDownEvent, _handler, RoutingStrategies.Tunnel);
            topLevel.Closed += OnClosed;
        }

        public List<KeyScopeBinding[]> Stack { get; }

        public void Detach()
        {
            _topLevel.RemoveHandler(InputElement.KeyDownEvent, _handler);
            _topLevel.Closed -= OnClosed;
        }

        void OnClosed(object? sender, EventArgs e)
        {
            _owner._entries.Remove(_topLevel);
            Detach();
        }
    }

    sealed class ScopeHandle(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}
