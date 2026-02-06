using System;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.Settings;

namespace Mdk.Hub.Features.Shell;

/// <summary>
/// Manages the state and visibility of easter egg features based on date and user preferences.
/// </summary>
[Singleton<IEasterEggService>]
public class EasterEggService : IEasterEggService
{
    readonly ISettings _settings;
    bool _disabledForever;
    DateTime? _disabledUntil;

    /// <summary>
    /// Initializes a new instance of the <see cref="EasterEggService"/> class.
    /// </summary>
    /// <param name="settings">The settings service to persist easter egg preferences.</param>
    public EasterEggService(ISettings settings)
    {
        _settings = settings;

        // Load initial settings
        LoadSettings();
        
        // Subscribe to settings changes
        _settings.SettingsChanged += OnSettingsChanged;
    }
    
    void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        // Reload easter egg state if HubSettings changed
        if (e.Key == SettingsKeys.HubSettings)
        {
            LoadSettings();
            ActiveChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    
    void LoadSettings()
    {
        var hubSettings = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings());
        _disabledForever = hubSettings.EasterEggDisabledForever;
        var disabledUntilTicks = hubSettings.EasterEggDisabledUntilTicks;
        if (disabledUntilTicks > 0)
            _disabledUntil = new DateTime(disabledUntilTicks);
        else
            _disabledUntil = null;
    }

    /// <summary>
    /// Occurs when the activation state of the easter egg changes.
    /// </summary>
    public event EventHandler? ActiveChanged;

    /// <summary>
    /// Gets a value indicating whether the easter egg is currently active.
    /// </summary>
    public bool IsActive
    {
        get
        {
#if DEBUG_EASTER_EGG
            var isEasterEggDay = true;
#else
            var today = DateTime.Now;
            var isEasterEggDay = today is { Month: 10, Day: 23 }; // Space Engineers Early Access release (Oct 23, 2013)
#endif
            var isDisabled = _disabledForever || (_disabledUntil.HasValue && DateTime.Now < _disabledUntil.Value);

            return isEasterEggDay && !isDisabled;
        }
    }

    /// <summary>
    /// Disables the easter egg for the specified duration.
    /// </summary>
    /// <param name="duration">The time span for which the easter egg should be disabled.</param>
    public void DisableFor(TimeSpan duration)
    {
        _disabledUntil = DateTime.Now + duration;
        var hubSettings = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings());
        hubSettings.EasterEggDisabledUntilTicks = _disabledUntil.Value.Ticks;
        _settings.SetValue(SettingsKeys.HubSettings, hubSettings);
        ActiveChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Disables the easter egg permanently.
    /// </summary>
    public void DisableForever()
    {
        _disabledForever = true;
        var hubSettings = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings());
        hubSettings.EasterEggDisabledForever = true;
        _settings.SetValue(SettingsKeys.HubSettings, hubSettings);
        ActiveChanged?.Invoke(this, EventArgs.Empty);
    }
}

