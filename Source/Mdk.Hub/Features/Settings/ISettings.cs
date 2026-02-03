using System;

namespace Mdk.Hub.Features.Settings;

public interface ISettings
{
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
    
    T GetValue<T>(string key) where T : struct;
    T GetValue<T>(string key, T defaultValue) where T : struct;
    void SetValue<T>(string key, T value) where T : struct;
}

public class SettingsChangedEventArgs(string key) : EventArgs
{
    public string Key { get; } = key;
}
