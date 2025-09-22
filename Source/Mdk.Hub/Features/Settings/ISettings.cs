namespace Mdk.Hub.Features.Settings;

public interface ISettings
{
    T GetValue<T>(string key);
    T GetValue<T>(string key, T defaultValue);
    void SetValue<T>(string key, T value);
}