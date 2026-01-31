using System;
using Mal.DependencyInjection;
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

        // Load settings
        _disabledForever = _settings.GetValue("EasterEggDisabledForever", false);
        var disabledUntilTicks = _settings.GetValue("EasterEggDisabledUntilTicks", 0L);
        if (disabledUntilTicks > 0)
            _disabledUntil = new DateTime(disabledUntilTicks);
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
        _settings.SetValue("EasterEggDisabledUntilTicks", _disabledUntil.Value.Ticks);
        ActiveChanged?.Invoke(this, EventArgs.Empty);
    }

    public void DisableForever()
    {
        _disabledForever = true;
        _settings.SetValue("EasterEggDisabledForever", true);
        ActiveChanged?.Invoke(this, EventArgs.Empty);
    }
}
