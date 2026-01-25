using System.Collections.Generic;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Options;

public record ComboBoxOption(string Value, string Display);

public class ConfigurationSectionViewModel : ViewModel
{
    public static List<ComboBoxOption> InteractiveOptionsList { get; } = new()
    {
        new("OpenHub", "Open Hub"),
        new("ShowNotification", "Show Notification"),
        new("DoNothing", "Do Nothing")
    };
    
    public static List<ComboBoxOption> MinifyOptionsList { get; } = new()
    {
        new("none", "None"),
        new("trim", "Trim"),
        new("stripcomments", "Strip Comments"),
        new("lite", "Lite"),
        new("full", "Full")
    };

    public static List<ComboBoxOption> MinifyExtraOptionsList { get; } = new()
    {
        new("none", "None"),
        new("nomembertrimming", "No Member Trimming")
    };

    public static List<ComboBoxOption> TraceOptionsList { get; } = new()
    {
        new("false", "Off"),
        new("true", "On")
    };

    ComboBoxOption? _interactive;
    public ComboBoxOption? Interactive
    {
        get => _interactive;
        set => SetProperty(ref _interactive, value);
    }
    
    string _outputPath = string.Empty;
    public string OutputPath
    {
        get => _outputPath;
        set => SetProperty(ref _outputPath, value);
    }
    
    string _binaryPath = string.Empty;
    public string BinaryPath
    {
        get => _binaryPath;
        set => SetProperty(ref _binaryPath, value);
    }
    
    ComboBoxOption? _minify;
    public ComboBoxOption? Minify
    {
        get => _minify;
        set => SetProperty(ref _minify, value);
    }
    
    ComboBoxOption? _minifyExtraOptions;
    public ComboBoxOption? MinifyExtraOptions
    {
        get => _minifyExtraOptions;
        set => SetProperty(ref _minifyExtraOptions, value);
    }
    
    ComboBoxOption? _trace;
    public ComboBoxOption? Trace
    {
        get => _trace;
        set => SetProperty(ref _trace, value);
    }
    
    string _ignores = string.Empty;
    public string Ignores
    {
        get => _ignores;
        set => SetProperty(ref _ignores, value);
    }
    
    string _namespaces = string.Empty;
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

