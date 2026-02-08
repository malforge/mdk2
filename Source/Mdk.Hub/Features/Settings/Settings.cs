using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.Storage;

namespace Mdk.Hub.Features.Settings;

/// <summary>
/// JSON-based settings store with caching support for application configuration.
/// </summary>
[Singleton<ISettings>]
public class Settings : ISettings
{
    readonly JsonObject _settings = new();
    readonly Dictionary<string, object> _cache = new();
    readonly string _settingsFileName;
    readonly IFileStorageService _fileStorage;

    /// <summary>
    /// Occurs when a setting value is changed.
    /// </summary>
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="Settings"/> class and loads settings from disk.
    /// </summary>
    public Settings(IFileStorageService fileStorage)
    {
        _fileStorage = fileStorage;
        _settingsFileName = _fileStorage.GetApplicationDataPath("settings.json");
        if (_fileStorage.FileExists(_settingsFileName))
        {
            try
            {
                var json = _fileStorage.ReadAllText(_settingsFileName);
                _settings = JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
            }
            catch (JsonException)
            {
                _settings = new JsonObject();
            }
        }
    }

    /// <summary>
    /// Attempts to get a setting value of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="value">When this method returns, contains the setting value if found; otherwise, the default value.</param>
    /// <returns><c>true</c> if the setting was found; otherwise, <c>false</c>.</returns>
    public bool TryGetValue<T>(string key, out T value) where T : struct
    {
        // Check cache first - only use if type matches exactly (struct is copied by value)
        if (_cache.TryGetValue(key, out var cachedValue) && cachedValue is T typedCached)
        {
            value = typedCached;
            return true;
        }

        // Not in cache or wrong type, deserialize
        if (_settings.TryGetPropertyValue(key, out var jsonValue))
        {
            var deserialized = jsonValue.Deserialize<T>()!;
            _cache[key] = deserialized;
            value = deserialized;
            return true;
        }

        value = default!;
        return false;
    }
    
    /// <summary>
    /// Gets a setting value of the specified type, or the default value if not found.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <returns>The setting value if found; otherwise, the default value for the type.</returns>
    public T GetValue<T>(string key) where T : struct
    {
        // Check cache first - only use if type matches exactly (struct is copied by value)
        if (_cache.TryGetValue(key, out var cachedValue) && cachedValue is T typedCached)
            return typedCached;

        // Not in cache or wrong type, deserialize
        if (_settings.TryGetPropertyValue(key, out var jsonValue))
        {
            var deserialized = jsonValue.Deserialize<T>()!;
            _cache[key] = deserialized;
            return deserialized;
        }
        
        return default;
    }

    /// <summary>
    /// Gets a setting value of the specified type, or a custom default value if not found.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="defaultValue">The default value to return if the setting is not found.</param>
    /// <returns>The setting value if found; otherwise, <paramref name="defaultValue"/>.</returns>
    public T GetValue<T>(string key, T defaultValue) where T : struct
    {
        // Check cache first - only use if type matches exactly (struct is copied by value)
        if (_cache.TryGetValue(key, out var cachedValue) && cachedValue is T typedCached)
            return typedCached;

        // Not in cache or wrong type, deserialize
        if (_settings.TryGetPropertyValue(key, out var jsonValue))
        {
            var deserialized = jsonValue.Deserialize<T>()!;
            _cache[key] = deserialized;
            return deserialized;
        }
        
        // Not found, cache and return default (struct is copied by value)
        _cache[key] = defaultValue;
        return defaultValue;
    }

    /// <summary>
    /// Sets a setting value and persists it to disk.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The value to set.</param>
    public void SetValue<T>(string key, T value) where T : struct
    {
        var jsonValue = JsonSerializer.SerializeToNode(value);
        _settings[key] = jsonValue;
        // Cache value (struct is copied by value, no mutation concerns)
        _cache[key] = value;
        SaveSettings();
        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(key));
    }

    void SaveSettings()
    {
        var directory = Path.GetDirectoryName(_settingsFileName);
        if (!string.IsNullOrEmpty(directory) && !_fileStorage.DirectoryExists(directory))
            _fileStorage.CreateDirectory(directory);
        var json = _settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        _fileStorage.WriteAllText(_settingsFileName, json);
    }
}
