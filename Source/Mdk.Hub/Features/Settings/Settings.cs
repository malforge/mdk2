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
    readonly string _settingsFileName;

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

    public T GetValue<T>(string key)
    {
        if (_settings.TryGetPropertyValue(key, out var jsonValue))
            return jsonValue.Deserialize<T>()!;
        throw new KeyNotFoundException($"Key '{key}' not found in settings.");
    }

    public T GetValue<T>(string key, T defaultValue)
    {
        if (_settings.TryGetPropertyValue(key, out var jsonValue))
            return jsonValue.Deserialize<T>()!;
        return defaultValue;
    }

    public void SetValue<T>(string key, T value)
    {
        var jsonValue = JsonSerializer.SerializeToNode(value);
        _settings[key] = jsonValue;
        SaveSettings();
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