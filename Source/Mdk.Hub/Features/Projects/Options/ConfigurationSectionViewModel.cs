using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Mdk.Hub.Features.Projects.Options;

public record ComboBoxOption(string Value, string Display);

public partial class ConfigurationSectionViewModel : ObservableObject
{
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

    [ObservableProperty]
    string _outputPath = string.Empty;
    
    [ObservableProperty]
    string _binaryPath = string.Empty;
    
    [ObservableProperty]
    ComboBoxOption? _minify;
    
    [ObservableProperty]
    ComboBoxOption? _minifyExtraOptions;
    
    [ObservableProperty]
    ComboBoxOption? _trace;
    
    [ObservableProperty]
    string _ignores = string.Empty;
    
    [ObservableProperty]
    string _namespaces = string.Empty;

    public void Clear()
    {
        OutputPath = string.Empty;
        BinaryPath = string.Empty;
        Minify = null;
        MinifyExtraOptions = null;
        Trace = null;
        Ignores = string.Empty;
        Namespaces = string.Empty;
    }
}

