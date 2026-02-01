namespace Mdk.Hub.Features.Settings;

/// <summary>
/// Constants for settings keys used with ISettings.
/// These keys are persisted in JSON and must remain stable for backwards compatibility.
/// </summary>
public static class SettingsKeys
{
    public const string HubSettings = nameof(HubSettings);
    public const string MainWindowSettings = nameof(MainWindowSettings);
}
