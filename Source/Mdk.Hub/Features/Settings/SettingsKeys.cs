namespace Mdk.Hub.Features.Settings;

/// <summary>
/// Constants for settings keys used with ISettings.
/// These keys are persisted in JSON and must remain stable for backwards compatibility.
/// </summary>
public static class SettingsKeys
{
    /// <summary>
    ///     Settings key for hub-wide settings.
    /// </summary>
    public const string HubSettings = nameof(HubSettings);
    
    /// <summary>
    ///     Settings key for main window state and preferences.
    /// </summary>
    public const string MainWindowSettings = nameof(MainWindowSettings);
}

