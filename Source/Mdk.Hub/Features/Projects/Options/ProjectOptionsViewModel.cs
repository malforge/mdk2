using System;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mdk.Hub.Features.Projects.Configuration;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Options;

[ViewModelFor<ProjectOptionsView>]
public partial class ProjectOptionsViewModel : ObservableObject
{
    readonly IProjectService _projectService;
    readonly string _projectPath;
    readonly Action _onClose;
    ProjectConfiguration? _configuration;
    string? _defaultBinaryPath;
    bool _defaultBinaryPathLoaded;

    public ConfigurationSectionViewModel MainConfig { get; } = new();
    public ConfigurationSectionViewModel LocalConfig { get; } = new();

    public ProjectOptionsViewModel(string projectPath, IProjectService projectService, Action onClose)
    {
        _projectPath = projectPath;
        _projectService = projectService;
        _onClose = onClose;
        
        // Subscribe to Local property changes to update override indicators
        LocalConfig.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ConfigurationSectionViewModel.OutputPath))
                OnPropertyChanged(nameof(IsOutputOverridden));
            else if (e.PropertyName == nameof(ConfigurationSectionViewModel.BinaryPath))
                OnPropertyChanged(nameof(IsBinaryPathOverridden));
            else if (e.PropertyName == nameof(ConfigurationSectionViewModel.Minify))
                OnPropertyChanged(nameof(IsMinifyOverridden));
            else if (e.PropertyName == nameof(ConfigurationSectionViewModel.MinifyExtraOptions))
                OnPropertyChanged(nameof(IsMinifyExtraOptionsOverridden));
            else if (e.PropertyName == nameof(ConfigurationSectionViewModel.Trace))
                OnPropertyChanged(nameof(IsTraceOverridden));
            else if (e.PropertyName == nameof(ConfigurationSectionViewModel.Ignores))
                OnPropertyChanged(nameof(IsIgnoresOverridden));
            else if (e.PropertyName == nameof(ConfigurationSectionViewModel.Namespaces))
                OnPropertyChanged(nameof(IsNamespacesOverridden));
        };
        
        LoadConfiguration();
    }

    public string ProjectName => Path.GetFileNameWithoutExtension(_projectPath);
    
    public bool IsProgrammableBlock => _configuration?.Type.Value?.Equals("programmableblock", StringComparison.OrdinalIgnoreCase) ?? true;
    
    public string? DefaultOutputPath => _configuration?.GetResolvedOutputPath();
    
    public string? DefaultBinaryPath
    {
        get
        {
            if (_defaultBinaryPathLoaded)
                return _defaultBinaryPath;
                
            _defaultBinaryPathLoaded = true;
            
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    var se = new Utility.SpaceEngineers();
                    _defaultBinaryPath = se.GetInstallPath("Bin64");
                }
                catch
                {
                    // Failed to find SE installation
                    _defaultBinaryPath = null;
                }
            }
            else
            {
                _defaultBinaryPath = null;
            }
            
            return _defaultBinaryPath;
        }
    }
    
    public bool IsOutputOverridden => !string.IsNullOrWhiteSpace(LocalConfig.OutputPath);
    public bool IsBinaryPathOverridden => !string.IsNullOrWhiteSpace(LocalConfig.BinaryPath);
    public bool IsMinifyOverridden => LocalConfig.Minify != null;
    public bool IsMinifyExtraOptionsOverridden => LocalConfig.MinifyExtraOptions != null;
    public bool IsTraceOverridden => LocalConfig.Trace != null;
    public bool IsIgnoresOverridden => !string.IsNullOrWhiteSpace(LocalConfig.Ignores);
    public bool IsNamespacesOverridden => !string.IsNullOrWhiteSpace(LocalConfig.Namespaces);

    void LoadConfiguration()
    {
        _configuration = _projectService.LoadConfiguration(_projectPath);
        OnPropertyChanged(nameof(IsProgrammableBlock));
        
        // Set defaults for Main
        MainConfig.Minify = ConfigurationSectionViewModel.MinifyOptionsList[0]; // "none"
        MainConfig.MinifyExtraOptions = ConfigurationSectionViewModel.MinifyExtraOptionsList[0]; // "none"
        MainConfig.Trace = ConfigurationSectionViewModel.TraceOptionsList[0]; // "false"
        MainConfig.Namespaces = "IngameScript";
        
        if (_configuration == null)
            return;

        // Load Main INI values (overriding defaults if present)
        if (_configuration.MainIni != null)
        {
            var section = _configuration.MainIni["mdk"];
            MainConfig.OutputPath = section.TryGet("output", out string? output) ? output : "auto";
            MainConfig.BinaryPath = section.TryGet("binarypath", out string? binary) ? binary : "auto";
            if (section.TryGet("minify", out string? minify))
                MainConfig.Minify = ConfigurationSectionViewModel.MinifyOptionsList.FirstOrDefault(o => o.Value == minify) ?? MainConfig.Minify;
            if (section.TryGet("minifyextraoptions", out string? minifyExtra))
                MainConfig.MinifyExtraOptions = ConfigurationSectionViewModel.MinifyExtraOptionsList.FirstOrDefault(o => o.Value == minifyExtra) ?? MainConfig.MinifyExtraOptions;
            if (section.TryGet("trace", out string? trace))
                MainConfig.Trace = ConfigurationSectionViewModel.TraceOptionsList.FirstOrDefault(o => o.Value == trace) ?? MainConfig.Trace;
            MainConfig.Ignores = section.TryGet("ignores", out string? ignores) ? ignores : string.Empty;
            MainConfig.Namespaces = section.TryGet("namespaces", out string? namespaces) && !string.IsNullOrWhiteSpace(namespaces) ? namespaces : MainConfig.Namespaces;
        }

        // Load Local INI values
        if (_configuration.LocalIni != null)
        {
            var section = _configuration.LocalIni["mdk"];
            LocalConfig.OutputPath = section.TryGet("output", out string? output) ? output : string.Empty;
            LocalConfig.BinaryPath = section.TryGet("binarypath", out string? binary) ? binary : string.Empty;
            if (section.TryGet("minify", out string? minify))
                LocalConfig.Minify = ConfigurationSectionViewModel.MinifyOptionsList.FirstOrDefault(o => o.Value == minify);
            if (section.TryGet("minifyextraoptions", out string? minifyExtra))
                LocalConfig.MinifyExtraOptions = ConfigurationSectionViewModel.MinifyExtraOptionsList.FirstOrDefault(o => o.Value == minifyExtra);
            if (section.TryGet("trace", out string? trace))
                LocalConfig.Trace = ConfigurationSectionViewModel.TraceOptionsList.FirstOrDefault(o => o.Value == trace);
            LocalConfig.Ignores = section.TryGet("ignores", out string? ignores) ? ignores : string.Empty;
            LocalConfig.Namespaces = section.TryGet("namespaces", out string? namespaces) ? namespaces : string.Empty;
        }
    }

    [RelayCommand]
    void Save()
    {
        // Save both Main and Local INI files
        _projectService.SaveConfiguration(_projectPath, 
            MainConfig.OutputPath, MainConfig.BinaryPath, 
            MainConfig.Minify?.Value ?? "none", MainConfig.MinifyExtraOptions?.Value ?? "none", 
            MainConfig.Trace?.Value ?? "false", MainConfig.Ignores, MainConfig.Namespaces, saveToLocal: false);
        _projectService.SaveConfiguration(_projectPath, 
            LocalConfig.OutputPath, LocalConfig.BinaryPath, 
            LocalConfig.Minify?.Value ?? string.Empty, LocalConfig.MinifyExtraOptions?.Value ?? string.Empty, 
            LocalConfig.Trace?.Value ?? string.Empty, LocalConfig.Ignores, LocalConfig.Namespaces, saveToLocal: true);
        
        _onClose();
    }

    [RelayCommand]
    void Cancel()
    {
        _onClose();
    }

    [RelayCommand]
    void ClearLocalOutputPath() => LocalConfig.OutputPath = string.Empty;

    [RelayCommand]
    void ClearLocalBinaryPath() => LocalConfig.BinaryPath = string.Empty;

    [RelayCommand]
    void ClearLocalMinify() => LocalConfig.Minify = null;

    [RelayCommand]
    void ClearLocalMinifyExtraOptions() => LocalConfig.MinifyExtraOptions = null;

    [RelayCommand]
    void ClearLocalTrace() => LocalConfig.Trace = null;

    [RelayCommand]
    void ClearLocalIgnores() => LocalConfig.Ignores = string.Empty;

    [RelayCommand]
    void ClearLocalNamespaces() => LocalConfig.Namespaces = string.Empty;
}
