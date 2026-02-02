using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mal.DependencyInjection;

namespace Mdk.Hub.Features.Settings;

[Singleton<ISettings>]
public class Settings : ISettings
{
    readonly JsonObject _settings = new();
    readonly Dictionary<string, object> _cache = new();
    readonly string _settingsFileName;

    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    public Settings()
    {
        _settingsFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MDK2/Hub/settings.json");
        if (File.Exists(_settingsFileName))
        {
            try
            {
                var json = File.ReadAllText(_settingsFileName);
                _settings = JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
            }
            catch (JsonException)
            {
                _settings = new JsonObject();
            }
        }
    }

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
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory!);
        var json = _settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsFileName, json);
    }
}