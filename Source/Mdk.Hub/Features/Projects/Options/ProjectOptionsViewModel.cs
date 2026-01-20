using System;
using System.IO;
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

    // Main INI values (mdk.ini - shared, in source control)
    [ObservableProperty]
    string _mainOutputPath = string.Empty;
    
    [ObservableProperty]
    string _mainBinaryPath = string.Empty;
    
    [ObservableProperty]
    string _mainMinify = string.Empty;
    
    [ObservableProperty]
    string _mainMinifyExtraOptions = string.Empty;
    
    [ObservableProperty]
    string _mainTrace = string.Empty;
    
    [ObservableProperty]
    string _mainIgnores = string.Empty;
    
    [ObservableProperty]
    string _mainNamespaces = string.Empty;

    // Local INI values (mdk.local.ini - local overrides, gitignored)
    [ObservableProperty]
    string _localOutputPath = string.Empty;
    
    [ObservableProperty]
    string _localBinaryPath = string.Empty;
    
    [ObservableProperty]
    string _localMinify = string.Empty;
    
    [ObservableProperty]
    string _localMinifyExtraOptions = string.Empty;
    
    [ObservableProperty]
    string _localTrace = string.Empty;
    
    [ObservableProperty]
    string _localIgnores = string.Empty;
    
    [ObservableProperty]
    string _localNamespaces = string.Empty;

    public ProjectOptionsViewModel(string projectPath, IProjectService projectService, Action onClose)
    {
        _projectPath = projectPath;
        _projectService = projectService;
        _onClose = onClose;
        
        LoadConfiguration();
    }

    public string ProjectName => Path.GetFileNameWithoutExtension(_projectPath);
    
    public bool IsOutputOverridden => !string.IsNullOrWhiteSpace(LocalOutputPath);
    public bool IsBinaryPathOverridden => !string.IsNullOrWhiteSpace(LocalBinaryPath);
    public bool IsMinifyOverridden => !string.IsNullOrWhiteSpace(LocalMinify);
    public bool IsMinifyExtraOptionsOverridden => !string.IsNullOrWhiteSpace(LocalMinifyExtraOptions);
    public bool IsTraceOverridden => !string.IsNullOrWhiteSpace(LocalTrace);
    public bool IsIgnoresOverridden => !string.IsNullOrWhiteSpace(LocalIgnores);
    public bool IsNamespacesOverridden => !string.IsNullOrWhiteSpace(LocalNamespaces);

    void LoadConfiguration()
    {
        _configuration = _projectService.LoadConfiguration(_projectPath);
        if (_configuration == null)
            return;

        // Load Main INI values
        if (_configuration.MainIni != null)
        {
            var section = _configuration.MainIni["mdk"];
            MainOutputPath = section.TryGet("output", out string? output) ? output : string.Empty;
            MainBinaryPath = section.TryGet("binarypath", out string? binary) ? binary : string.Empty;
            MainMinify = section.TryGet("minify", out string? minify) ? minify : string.Empty;
            MainMinifyExtraOptions = section.TryGet("minifyextraoptions", out string? minifyExtra) ? minifyExtra : string.Empty;
            MainTrace = section.TryGet("trace", out string? trace) ? trace : string.Empty;
            MainIgnores = section.TryGet("ignores", out string? ignores) ? ignores : string.Empty;
            MainNamespaces = section.TryGet("namespaces", out string? namespaces) ? namespaces : string.Empty;
        }

        // Load Local INI values
        if (_configuration.LocalIni != null)
        {
            var section = _configuration.LocalIni["mdk"];
            LocalOutputPath = section.TryGet("output", out string? output) ? output : string.Empty;
            LocalBinaryPath = section.TryGet("binarypath", out string? binary) ? binary : string.Empty;
            LocalMinify = section.TryGet("minify", out string? minify) ? minify : string.Empty;
            LocalMinifyExtraOptions = section.TryGet("minifyextraoptions", out string? minifyExtra) ? minifyExtra : string.Empty;
            LocalTrace = section.TryGet("trace", out string? trace) ? trace : string.Empty;
            LocalIgnores = section.TryGet("ignores", out string? ignores) ? ignores : string.Empty;
            LocalNamespaces = section.TryGet("namespaces", out string? namespaces) ? namespaces : string.Empty;
        }
        
        // Notify override properties
        OnPropertyChanged(nameof(IsOutputOverridden));
        OnPropertyChanged(nameof(IsBinaryPathOverridden));
        OnPropertyChanged(nameof(IsMinifyOverridden));
        OnPropertyChanged(nameof(IsMinifyExtraOptionsOverridden));
        OnPropertyChanged(nameof(IsTraceOverridden));
        OnPropertyChanged(nameof(IsIgnoresOverridden));
        OnPropertyChanged(nameof(IsNamespacesOverridden));
    }

    [RelayCommand]
    void Save()
    {
        // Save both Main and Local INI files
        _projectService.SaveConfiguration(_projectPath, MainOutputPath, MainBinaryPath, MainMinify, MainMinifyExtraOptions, MainTrace, MainIgnores, MainNamespaces, saveToLocal: false);
        _projectService.SaveConfiguration(_projectPath, LocalOutputPath, LocalBinaryPath, LocalMinify, LocalMinifyExtraOptions, LocalTrace, LocalIgnores, LocalNamespaces, saveToLocal: true);
        
        _onClose();
    }

    [RelayCommand]
    void Cancel()
    {
        _onClose();
    }
}
