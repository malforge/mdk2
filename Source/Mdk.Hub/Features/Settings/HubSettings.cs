using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Mdk.Hub.Features.Settings;

/// <summary>
///     Configuration object for MDK Hub global settings.
/// </summary>
public struct HubSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="HubSettings"/> struct.
    /// </summary>
    public HubSettings()
    {
    }

    /// <summary>
    ///     Gets or sets the custom auto output path for ingame scripts.
    ///     When null or "auto", uses the default behavior (%AppData%/SpaceEngineers/IngameScripts/local).
    /// </summary>
    public string CustomAutoScriptOutputPath { get; set; } = "auto";

    /// <summary>
    ///     Gets or sets the custom auto output path for mods.
    ///     When null or "auto", uses the default behavior (%AppData%/SpaceEngineers/Mods).
    /// </summary>
    public string CustomAutoModOutputPath { get; set; } = "auto";

    /// <summary>
    ///     Gets or sets the custom auto binary path.
    ///     When null or "auto", uses the default behavior (game bin folder).
    /// </summary>
    public string CustomAutoBinaryPath { get; set; } = "auto";

    /// <summary>
    ///     Gets or sets whether to include prerelease versions when checking for updates.
    /// </summary>
    public bool IncludePrereleaseUpdates { get; set; }

    /// <summary>
    ///     Gets or sets whether the user has been prompted about enabling prerelease updates.
    ///     Used to avoid repeatedly asking when running a prerelease version.
    /// </summary>
    public bool HasPromptedForPrereleaseUpdates { get; set; }

    /// <summary>
    ///     Gets or sets the list of dismissed announcement IDs.
    /// </summary>
    public ImmutableArray<string> DismissedAnnouncementIds { get; set; } = ImmutableArray<string>.Empty;

    /// <summary>
    ///     Gets or sets the last used location for creating Ingame Script projects.
    /// </summary>
    public string? LastIngameScriptLocation { get; set; }

    /// <summary>
    ///     Gets or sets the last used location for creating Mod projects.
    /// </summary>
    public string? LastModLocation { get; set; }

    /// <summary>
    ///     Gets or sets whether the Easter egg is disabled forever.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool EasterEggDisabledForever { get; set; }

    /// <summary>
    ///     Gets or sets the ticks value until which the Easter egg is disabled.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long EasterEggDisabledUntilTicks { get; set; }

    /// <summary>
    ///     Gets or sets the last selected project path.
    /// </summary>
    public string LastSelectedProject { get; set; } = string.Empty;
}

