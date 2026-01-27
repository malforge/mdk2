using System.Collections.Generic;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Options;

public record ComboBoxOption(string Value, string Display);

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

    public static List<ComboBoxOption> InteractiveOptionsList { get; } = new()
    {
        new ComboBoxOption("OpenHub", "Open Hub"),
        new ComboBoxOption("ShowNotification", "Show Notification"),
        new ComboBoxOption("DoNothing", "Do Nothing")
    };

    public static List<ComboBoxOption> MinifyOptionsList { get; } = new()
    {
        new ComboBoxOption("none", "None"),
        new ComboBoxOption("trim", "Trim"),
        new ComboBoxOption("stripcomments", "Strip Comments"),
        new ComboBoxOption("lite", "Lite"),
        new ComboBoxOption("full", "Full")
    };

    public static List<ComboBoxOption> MinifyExtraOptionsList { get; } = new()
    {
        new ComboBoxOption("none", "None"),
        new ComboBoxOption("nomembertrimming", "No Member Trimming")
    };

    public static List<ComboBoxOption> TraceOptionsList { get; } = new()
    {
        new ComboBoxOption("false", "Off"),
        new ComboBoxOption("true", "On")
    };

    public ComboBoxOption? Interactive
    {
        get => _interactive;
        set => SetProperty(ref _interactive, value);
    }

    public string OutputPath
    {
        get => _outputPath;
        set => SetProperty(ref _outputPath, value);
    }

    public string BinaryPath
    {
        get => _binaryPath;
        set => SetProperty(ref _binaryPath, value);
    }

    public ComboBoxOption? Minify
    {
        get => _minify;
        set => SetProperty(ref _minify, value);
    }

    public ComboBoxOption? MinifyExtraOptions
    {
        get => _minifyExtraOptions;
        set => SetProperty(ref _minifyExtraOptions, value);
    }

    public ComboBoxOption? Trace
    {
        get => _trace;
        set => SetProperty(ref _trace, value);
    }

    public string Ignores
    {
        get => _ignores;
        set => SetProperty(ref _ignores, value);
    }

    public string Namespaces
    {
        get => _namespaces;
        set => SetProperty(ref _namespaces, value);
    }

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