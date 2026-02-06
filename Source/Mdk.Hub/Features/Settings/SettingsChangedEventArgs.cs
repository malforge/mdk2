using System;

namespace Mdk.Hub.Features.Settings;

/// <summary>
/// Provides data for the settings changed event.
/// </summary>
/// <param name="key">The key of the setting that changed.</param>
public class SettingsChangedEventArgs(string key) : EventArgs
{
    /// <summary>
    /// Gets the key of the setting that changed.
    /// </summary>
    public string Key { get; } = key;
}