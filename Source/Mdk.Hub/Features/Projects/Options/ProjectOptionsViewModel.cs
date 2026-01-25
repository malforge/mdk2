using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Mdk.Hub.Features.Projects.Configuration;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects.Options;

[ViewModelFor<ProjectOptionsView>]
public class ProjectOptionsViewModel : ViewModel
{
    readonly IProjectService _projectService;
    readonly Mdk.Hub.Features.CommonDialogs.ICommonDialogs _commonDialogs;
    readonly IShell _shell;
    readonly CanonicalPath _projectPath;
    readonly Action<bool> _onClose; // bool parameter: true if saved, false if cancelled
    readonly Action? _onDirtyStateChanged;
    ProjectConfiguration? _configuration;
    string? _defaultBinaryPath;
    bool _defaultBinaryPathLoaded;
    
    // Store original values to detect changes
    string? _originalMainInteractive;
    string? _originalMainOutputPath;
    string? _originalMainBinaryPath;
    string? _originalMainMinify;
    string? _originalMainMinifyExtraOptions;
    string? _originalMainTrace;
    string? _originalMainIgnores;
    string? _originalMainNamespaces;
    string? _originalLocalInteractive;
    string? _originalLocalOutputPath;
    string? _originalLocalBinaryPath;
    string? _originalLocalMinify;
    string? _originalLocalMinifyExtraOptions;
    string? _originalLocalTrace;
    string? _originalLocalIgnores;
    string? _originalLocalNamespaces;
    
    readonly AsyncRelayCommand _saveCommand;
    readonly RelayCommand _cancelCommand;
    readonly RelayCommand _clearLocalInteractiveCommand;
    readonly RelayCommand _clearLocalOutputPathCommand;
    readonly RelayCommand _clearLocalBinaryPathCommand;
    readonly RelayCommand _clearLocalMinifyCommand;
    readonly RelayCommand _clearLocalMinifyExtraOptionsCommand;
    readonly RelayCommand _clearLocalTraceCommand;
    readonly RelayCommand _clearLocalIgnoresCommand;
    readonly RelayCommand _clearLocalNamespacesCommand;
    readonly RelayCommand _openGlobalSettingsCommand;

    public ConfigurationSectionViewModel MainConfig { get; } = new();
    public ConfigurationSectionViewModel LocalConfig { get; } = new();

    public ICommand SaveCommand => _saveCommand;
    public ICommand CancelCommand => _cancelCommand;
    public ICommand ClearLocalInteractiveCommand => _clearLocalInteractiveCommand;
    public ICommand ClearLocalOutputPathCommand => _clearLocalOutputPathCommand;
    public ICommand ClearLocalBinaryPathCommand => _clearLocalBinaryPathCommand;
    public ICommand ClearLocalMinifyCommand => _clearLocalMinifyCommand;
    public ICommand ClearLocalMinifyExtraOptionsCommand => _clearLocalMinifyExtraOptionsCommand;
    public ICommand ClearLocalTraceCommand => _clearLocalTraceCommand;
    public ICommand ClearLocalIgnoresCommand => _clearLocalIgnoresCommand;
    public ICommand ClearLocalNamespacesCommand => _clearLocalNamespacesCommand;
    public ICommand OpenGlobalSettingsCommand => _openGlobalSettingsCommand;

    public ProjectOptionsViewModel(string projectPath, IProjectService projectService, Mdk.Hub.Features.CommonDialogs.ICommonDialogs commonDialogs, IShell shell, Action<bool> onClose, Action? onDirtyStateChanged = null)
    {
        _projectPath = new CanonicalPath(projectPath);
        _projectService = projectService;
        _commonDialogs = commonDialogs;
        _shell = shell;
        _onClose = onClose;
        _onDirtyStateChanged = onDirtyStateChanged;
        
        _saveCommand = new AsyncRelayCommand(Save);
        _cancelCommand = new RelayCommand(Cancel);
        _clearLocalInteractiveCommand = new RelayCommand(ClearLocalInteractive);
        _clearLocalOutputPathCommand = new RelayCommand(ClearLocalOutputPath);
        _clearLocalBinaryPathCommand = new RelayCommand(ClearLocalBinaryPath);
        _clearLocalMinifyCommand = new RelayCommand(ClearLocalMinify);
        _clearLocalMinifyExtraOptionsCommand = new RelayCommand(ClearLocalMinifyExtraOptions);
        _clearLocalTraceCommand = new RelayCommand(ClearLocalTrace);
        _clearLocalIgnoresCommand = new RelayCommand(ClearLocalIgnores);
        _clearLocalNamespacesCommand = new RelayCommand(ClearLocalNamespaces);
        _openGlobalSettingsCommand = new RelayCommand(OpenGlobalSettings);
        
        // Subscribe to property changes to update override indicators and dirty state
        MainConfig.PropertyChanged += (_, e) =>
        {
            OnPropertyChanged(nameof(HasUnsavedChanges));
            NotifyDirtyStateChanged();
        };
        
        LocalConfig.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ConfigurationSectionViewModel.Interactive))
                OnPropertyChanged(nameof(IsInteractiveOverridden));
            else if (e.PropertyName == nameof(ConfigurationSectionViewModel.OutputPath))
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
                
            OnPropertyChanged(nameof(HasUnsavedChanges));
            NotifyDirtyStateChanged();
        };
        
        LoadConfiguration();
    }

    public string ProjectName => _projectPath.IsEmpty() ? string.Empty : Path.GetFileNameWithoutExtension(_projectPath.Value!);
    
    public bool IsProgrammableBlock => _configuration?.Type.Value?.Equals("programmableblock", StringComparison.OrdinalIgnoreCase) ?? true;
    
    public bool HasUnsavedChanges =>
        MainConfig.Interactive?.Value != _originalMainInteractive ||
        MainConfig.OutputPath != _originalMainOutputPath ||
        MainConfig.BinaryPath != _originalMainBinaryPath ||
        MainConfig.Minify?.Value != _originalMainMinify ||
        MainConfig.MinifyExtraOptions?.Value != _originalMainMinifyExtraOptions ||
        MainConfig.Trace?.Value != _originalMainTrace ||
        MainConfig.Ignores != _originalMainIgnores ||
        MainConfig.Namespaces != _originalMainNamespaces ||
        LocalConfig.Interactive?.Value != _originalLocalInteractive ||
        LocalConfig.OutputPath != _originalLocalOutputPath ||
        LocalConfig.BinaryPath != _originalLocalBinaryPath ||
        LocalConfig.Minify?.Value != _originalLocalMinify ||
        LocalConfig.MinifyExtraOptions?.Value != _originalLocalMinifyExtraOptions ||
        LocalConfig.Trace?.Value != _originalLocalTrace ||
        LocalConfig.Ignores != _originalLocalIgnores ||
        LocalConfig.Namespaces != _originalLocalNamespaces;
    
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
    
    public bool IsInteractiveOverridden => LocalConfig.Interactive != null;
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
        MainConfig.Interactive = ConfigurationSectionViewModel.InteractiveOptionsList[0]; // "OpenHub"
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
            if (section.TryGet("interactive", out string? interactive))
                MainConfig.Interactive = ConfigurationSectionViewModel.InteractiveOptionsList.FirstOrDefault(o => o.Value == interactive) ?? MainConfig.Interactive;
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
            if (section.TryGet("interactive", out string? interactive))
                LocalConfig.Interactive = ConfigurationSectionViewModel.InteractiveOptionsList.FirstOrDefault(o => o.Value == interactive);
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
        
        // If interactive is not set anywhere (neither Main nor Local), default LocalConfig to "ShowNotification"
        // This triggers unsaved changes and teaches users to explicitly save their preference
        bool hasInteractiveInMain = _configuration.MainIni != null && _configuration.MainIni["mdk"].TryGet("interactive", out string? _);
        bool hasInteractiveInLocal = LocalConfig.Interactive != null;
        if (!hasInteractiveInMain && !hasInteractiveInLocal)
        {
            LocalConfig.Interactive = ConfigurationSectionViewModel.InteractiveOptionsList.FirstOrDefault(o => o.Value == "ShowNotification");
        }
        
        // Store original values for dirty tracking
        _originalMainInteractive = MainConfig.Interactive?.Value;
        _originalMainOutputPath = MainConfig.OutputPath;
        _originalMainBinaryPath = MainConfig.BinaryPath;
        _originalMainMinify = MainConfig.Minify?.Value;
        _originalMainMinifyExtraOptions = MainConfig.MinifyExtraOptions?.Value;
        _originalMainTrace = MainConfig.Trace?.Value;
        _originalMainIgnores = MainConfig.Ignores;
        _originalMainNamespaces = MainConfig.Namespaces;
        _originalLocalInteractive = LocalConfig.Interactive?.Value;
        _originalLocalOutputPath = LocalConfig.OutputPath;
        _originalLocalBinaryPath = LocalConfig.BinaryPath;
        _originalLocalMinify = LocalConfig.Minify?.Value;
        _originalLocalMinifyExtraOptions = LocalConfig.MinifyExtraOptions?.Value;
        _originalLocalTrace = LocalConfig.Trace?.Value;
        _originalLocalIgnores = LocalConfig.Ignores;
        _originalLocalNamespaces = LocalConfig.Namespaces;
    }

    async Task Save()
    {
        try
        {
            // Save both Main and Local INI files
            await _projectService.SaveConfiguration(_projectPath, 
                MainConfig.Interactive?.Value ?? "OpenHub",
                MainConfig.OutputPath, MainConfig.BinaryPath, 
                MainConfig.Minify?.Value ?? "none", MainConfig.MinifyExtraOptions?.Value ?? "none", 
                MainConfig.Trace?.Value ?? "false", MainConfig.Ignores, MainConfig.Namespaces, saveToLocal: false);
            await _projectService.SaveConfiguration(_projectPath, 
                LocalConfig.Interactive?.Value ?? string.Empty,
                LocalConfig.OutputPath, LocalConfig.BinaryPath, 
                LocalConfig.Minify?.Value ?? string.Empty, LocalConfig.MinifyExtraOptions?.Value ?? string.Empty, 
                LocalConfig.Trace?.Value ?? string.Empty, LocalConfig.Ignores, LocalConfig.Namespaces, saveToLocal: true);
            
            _onClose(true); // Saved
        }
        catch (UnauthorizedAccessException ex)
        {
            await _commonDialogs.ShowAsync(new Mdk.Hub.Features.CommonDialogs.ConfirmationMessage
            {
                Title = "Permission Denied",
                Message = $"Cannot save configuration file. Access is denied.\n\nPlease check file permissions and try again.\n\nDetails: {ex.Message}",
                OkText = "OK",
                CancelText = ""
            });
        }
        catch (IOException ex)
        {
            await _commonDialogs.ShowAsync(new Mdk.Hub.Features.CommonDialogs.ConfirmationMessage
            {
                Title = "Save Failed",
                Message = $"Cannot save configuration file. The disk may be full or the file may be in use.\n\nDetails: {ex.Message}",
                OkText = "OK",
                CancelText = ""
            });
        }
    }
    
    void NotifyDirtyStateChanged()
    {
        _onDirtyStateChanged?.Invoke();
    }

    void Cancel()
    {
        _onClose(false); // Cancelled
    }

    void ClearLocalInteractive() => LocalConfig.Interactive = null;

    void ClearLocalOutputPath() => LocalConfig.OutputPath = string.Empty;

    void ClearLocalBinaryPath() => LocalConfig.BinaryPath = string.Empty;

    void ClearLocalMinify() => LocalConfig.Minify = null;

    void ClearLocalMinifyExtraOptions() => LocalConfig.MinifyExtraOptions = null;

    void ClearLocalTrace() => LocalConfig.Trace = null;

    void ClearLocalIgnores() => LocalConfig.Ignores = string.Empty;

    void ClearLocalNamespaces() => LocalConfig.Namespaces = string.Empty;

    void OpenGlobalSettings()
    {
        var viewModel = App.Container.Resolve<Mdk.Hub.Features.Settings.GlobalSettingsViewModel>();
        _shell.AddOverlay(viewModel);
    }
}
