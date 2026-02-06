using System;

namespace Mdk.Hub.Features.Settings;

/// <summary>
/// Provides access to application settings storage with change notifications.
/// </summary>
public interface ISettings
{
    /// <summary>
    /// Occurs when a setting value changes.
    /// </summary>
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
    
    /// <summary>
    /// Gets a setting value for the specified key.
    /// </summary>
    /// <typeparam name="T">The value type of the setting.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <returns>The setting value.</returns>
    T GetValue<T>(string key) where T : struct;
    
    /// <summary>
    /// Gets a setting value for the specified key, or returns the default value if the key does not exist.
    /// </summary>
    /// <typeparam name="T">The value type of the setting.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="defaultValue">The default value to return if the key does not exist.</param>
    /// <returns>The setting value or the default value.</returns>
    T GetValue<T>(string key, T defaultValue) where T : struct;
    
    /// <summary>
    /// Sets a setting value for the specified key.
    /// </summary>
    /// <typeparam name="T">The value type of the setting.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The value to set.</param>
    void SetValue<T>(string key, T value) where T : struct;
}