using Mal.DependencyInjection;
using Mdk.Hub.Features.Diagnostics;

namespace Mdk.Hub.Features.Settings;

/// <summary>
/// Provides typed access to global MDK Hub settings
/// </summary>
[Dependency<GlobalSettings>]
public class GlobalSettings
{
    readonly ISettings _settings;
    readonly ILogger _logger;

    public GlobalSettings(ISettings settings, ILogger logger)
    {
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the custom auto output path for ingame scripts.
    /// When null or "auto", uses the default behavior (%AppData%/SpaceEngineers/IngameScripts/local).
    /// </summary>
    public string CustomAutoScriptOutputPath
    {
        get
        {
            var value = _settings.GetValue<string?>("CustomAutoScriptOutputPath", null);
            if (string.IsNullOrWhiteSpace(value))
                return "auto";
            return value;
        }
        set
        {
            var normalizedValue = string.IsNullOrWhiteSpace(value) || value == "auto" ? "auto" : value;
            _settings.SetValue("CustomAutoScriptOutputPath", normalizedValue);
            _logger.Info($"Custom auto script output path set to: {(normalizedValue == "auto" ? "(default)" : normalizedValue)}");
        }
    }

    /// <summary>
    /// Gets or sets the custom auto output path for mods.
    /// When null or "auto", uses the default behavior (%AppData%/SpaceEngineers/Mods).
    /// </summary>
    public string CustomAutoModOutputPath
    {
        get
        {
            var value = _settings.GetValue<string?>("CustomAutoModOutputPath", null);
            if (string.IsNullOrWhiteSpace(value))
                return "auto";
            return value;
        }
        set
        {
            var normalizedValue = string.IsNullOrWhiteSpace(value) || value == "auto" ? "auto" : value;
            _settings.SetValue("CustomAutoModOutputPath", normalizedValue);
            _logger.Info($"Custom auto mod output path set to: {(normalizedValue == "auto" ? "(default)" : normalizedValue)}");
        }
    }

    /// <summary>
    /// Gets or sets the custom auto binary path.
    /// When null or "auto", uses the default behavior (game bin folder).
    /// </summary>
    public string CustomAutoBinaryPath
    {
        get
        {
            var value = _settings.GetValue<string?>("CustomAutoBinaryPath", null);
            if (string.IsNullOrWhiteSpace(value))
                return "auto";
            return value;
        }
        set
        {
            var normalizedValue = string.IsNullOrWhiteSpace(value) || value == "auto" ? "auto" : value;
            _settings.SetValue("CustomAutoBinaryPath", normalizedValue);
            _logger.Info($"Custom auto binary path set to: {(normalizedValue == "auto" ? "(default)" : normalizedValue)}");
        }
    }
}
