using System;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.Settings;

namespace Mdk.Hub.Features.Shell;

[Singleton<IEasterEggService>]
public class EasterEggService : IEasterEggService
{
    readonly ISettings _settings;
    bool _disabledForever;
    DateTime? _disabledUntil;

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

    public event EventHandler? ActiveChanged;

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

    public void DisableFor(TimeSpan duration)
    {
        _disabledUntil = DateTime.Now + duration;
        var hubSettings = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings());
        hubSettings.EasterEggDisabledUntilTicks = _disabledUntil.Value.Ticks;
        _settings.SetValue(SettingsKeys.HubSettings, hubSettings);
        ActiveChanged?.Invoke(this, EventArgs.Empty);
    }

    public void DisableForever()
    {
        _disabledForever = true;
        var hubSettings = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings());
        hubSettings.EasterEggDisabledForever = true;
        _settings.SetValue(SettingsKeys.HubSettings, hubSettings);
        ActiveChanged?.Invoke(this, EventArgs.Empty);
    }
}

