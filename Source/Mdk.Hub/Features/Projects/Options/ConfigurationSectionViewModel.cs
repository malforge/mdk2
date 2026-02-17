using System.Collections.Generic;
using System.Collections.Immutable;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Options;

/// <summary>
/// View model for the configuration section of a project, providing options for build settings, paths, and minification.
/// </summary>
public class ConfigurationSectionViewModel : ViewModel
{
    string _binaryPath = string.Empty;

    string _ignores = string.Empty;

    ComboBoxOption? _interactive;

    ComboBoxOption? _minify;

    ComboBoxOption? _minifyExtraOptions;

    string _namespaces = string.Empty;

    string _outputPath = string.Empty;

    ComboBoxOption? _trace;

    /// <summary>
    /// Gets the available options for interactive mode settings.
    /// </summary>
    public static List<ComboBoxOption> InteractiveOptionsList { get; } = new()
    {
        new ComboBoxOption("OpenHub", "Open Hub"),
        new ComboBoxOption("ShowNotification", "Show Notification"),
        new ComboBoxOption("DoNothing", "Do Nothing")
    };

    /// <summary>
    /// Gets the available options for minification levels.
    /// </summary>
    public static List<ComboBoxOption> MinifyOptionsList { get; } = new()
    {
        new ComboBoxOption("none", "None"),
        new ComboBoxOption("trim", "Trim"),
        new ComboBoxOption("stripcomments", "Strip Comments"),
        new ComboBoxOption("lite", "Lite"),
        new ComboBoxOption("full", "Full")
    };

    /// <summary>
    /// Gets the available extra options that can be applied during minification.
    /// </summary>
    public static List<ComboBoxOption> MinifyExtraOptionsList { get; } = new()
    {
        new ComboBoxOption("none", "None"),
        new ComboBoxOption("nomembertrimming", "No Member Trimming")
    };

    /// <summary>
    /// Gets the available options for trace logging (on/off).
    /// </summary>
    public static List<ComboBoxOption> TraceOptionsList { get; } = new()
    {
        new ComboBoxOption("false", "Off"),
        new ComboBoxOption("true", "On")
    };

    /// <summary>
    /// Gets or sets the selected interactive mode option.
    /// </summary>
    public ComboBoxOption? Interactive
    {
        get => _interactive;
        set => SetProperty(ref _interactive, value);
    }

    /// <summary>
    /// Gets or sets the output path for the built script or mod.
    /// </summary>
    public string OutputPath
    {
        get => _outputPath;
        set => SetProperty(ref _outputPath, value);
    }

    /// <summary>
    /// Gets or sets the path to the Space Engineers game binaries.
    /// </summary>
    public string BinaryPath
    {
        get => _binaryPath;
        set => SetProperty(ref _binaryPath, value);
    }

    /// <summary>
    /// Gets or sets the selected minification level option.
    /// </summary>
    public ComboBoxOption? Minify
    {
        get => _minify;
        set => SetProperty(ref _minify, value);
    }

    /// <summary>
    /// Gets or sets the selected minification extra options.
    /// </summary>
    public ComboBoxOption? MinifyExtraOptions
    {
        get => _minifyExtraOptions;
        set => SetProperty(ref _minifyExtraOptions, value);
    }

    /// <summary>
    /// Gets or sets the selected trace logging option.
    /// </summary>
    public ComboBoxOption? Trace
    {
        get => _trace;
        set => SetProperty(ref _trace, value);
    }

    /// <summary>
    /// Gets or sets the glob patterns for files to ignore during build.
    /// </summary>
    public string Ignores
    {
        get => _ignores;
        set => SetProperty(ref _ignores, value);
    }

    /// <summary>
    /// Gets or sets the namespaces to accept without warnings (for programmable block scripts).
    /// </summary>
    public string Namespaces
    {
        get => _namespaces;
        set => SetProperty(ref _namespaces, value);
    }

    /// <summary>
    /// Resets all configuration options to their default values.
    /// </summary>
    public void Clear()
    {
        Interactive = null;
        OutputPath = "auto";
        BinaryPath = "auto";
        Minify = null;
        MinifyExtraOptions = null;
        Trace = null;
        Ignores = string.Empty;
        Namespaces = string.Empty;
    }
}
