using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Projects.Configuration;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Settings;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects.Options;

[ViewModelFor<ProjectOptionsView>]
public class ProjectOptionsViewModel : ViewModel
{
    readonly RelayCommand _cancelCommand;
    readonly RelayCommand _clearLocalBinaryPathCommand;
    readonly RelayCommand _clearLocalIgnoresCommand;
    readonly RelayCommand _clearLocalInteractiveCommand;
    readonly RelayCommand _clearLocalMinifyCommand;
    readonly RelayCommand _clearLocalMinifyExtraOptionsCommand;
    readonly RelayCommand _clearLocalNamespacesCommand;
    readonly RelayCommand _clearLocalOutputPathCommand;
    readonly RelayCommand _clearLocalTraceCommand;
    readonly IShell _dialogShell;
    readonly ILogger _logger;
    readonly AsyncRelayCommand _normalizeConfigurationCommand;
    readonly Action<bool> _onClose; // bool parameter: true if saved, false if cancelled
    readonly Action? _onDirtyStateChanged;
    readonly RelayCommand _openGlobalSettingsCommand;
    readonly CanonicalPath _projectPath;
    readonly IProjectService _projectService;

    readonly AsyncRelayCommand _saveCommand;
    readonly IShell _shell;
    string? _defaultBinaryPath;
    bool _defaultBinaryPathLoaded;
    ProjectData? _projectData;

    public ProjectOptionsViewModel(string projectPath, IProjectService projectService, IShell dialogShell, IShell shell, ILogger logger, Action<bool> onClose, Action? onDirtyStateChanged = null)
    {
        _projectPath = new CanonicalPath(projectPath);
        _projectService = projectService;
        _dialogShell = dialogShell;
        _shell = shell;
        _logger = logger;
        _onClose = onClose;
        _onDirtyStateChanged = onDirtyStateChanged;

        _saveCommand = new AsyncRelayCommand(Save);
        _cancelCommand = new RelayCommand(Cancel);
        _normalizeConfigurationCommand = new AsyncRelayCommand(NormalizeConfigurationAsync);
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
        MainConfig.PropertyChanged += (_, _) =>
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

    public ConfigurationSectionViewModel MainConfig { get; } = new();
    public ConfigurationSectionViewModel LocalConfig { get; } = new();

    public ICommand SaveCommand => _saveCommand;
    public ICommand CancelCommand => _cancelCommand;
    public ICommand NormalizeConfigurationCommand => _normalizeConfigurationCommand;
    public ICommand ClearLocalInteractiveCommand => _clearLocalInteractiveCommand;
    public ICommand ClearLocalOutputPathCommand => _clearLocalOutputPathCommand;
    public ICommand ClearLocalBinaryPathCommand => _clearLocalBinaryPathCommand;
    public ICommand ClearLocalMinifyCommand => _clearLocalMinifyCommand;
    public ICommand ClearLocalMinifyExtraOptionsCommand => _clearLocalMinifyExtraOptionsCommand;
    public ICommand ClearLocalTraceCommand => _clearLocalTraceCommand;
    public ICommand ClearLocalIgnoresCommand => _clearLocalIgnoresCommand;
    public ICommand ClearLocalNamespacesCommand => _clearLocalNamespacesCommand;
    public ICommand OpenGlobalSettingsCommand => _openGlobalSettingsCommand;

    public string ProjectName => _projectPath.IsEmpty() ? string.Empty : Path.GetFileNameWithoutExtension(_projectPath.Value!);

    public bool IsProgrammableBlock => _projectData?.Config.GetEffective().Type == ProjectType.ProgrammableBlock;

    public bool IsStandardConfiguration
    {
        get
        {
            if (_projectData == null) return true;

            // Check Local has no "main" settings
            var local = _projectData.Config.Local;
            if (local != null)
            {
                if (local.Type != null)
                {
                    _logger.Info("Configuration non-standard: Local has Type");
                    return false;
                }
                if (local.Namespaces != null)
                {
                    _logger.Info("Configuration non-standard: Local has Namespaces");
                    return false;
                }
                if (local.Ignores != null)
                {
                    _logger.Info("Configuration non-standard: Local has Ignores");
                    return false;
                }
                if (local.Minify != null)
                {
                    _logger.Info("Configuration non-standard: Local has Minify");
                    return false;
                }
                if (local.MinifyExtraOptions != null)
                {
                    _logger.Info("Configuration non-standard: Local has MinifyExtraOptions");
                    return false;
                }
                if (local.Trace != null)
                {
                    _logger.Info("Configuration non-standard: Local has Trace");
                    return false;
                }
            }

            // Check Main has no "local" settings
            var main = _projectData.Config.Main;
            if (main != null)
            {
                if (main.Output != null)
                {
                    _logger.Info($"Configuration non-standard: Main has Output = {main.Output.Value}");
                    return false;
                }
                if (main.BinaryPath != null)
                {
                    _logger.Info($"Configuration non-standard: Main has BinaryPath = {main.BinaryPath.Value}");
                    return false;
                }
                if (main.Interactive != null)
                {
                    _logger.Info($"Configuration non-standard: Main has Interactive = {main.Interactive}");
                    return false;
                }
            }

            _logger.Debug($"Configuration is standard for project: {ProjectName}");
            return true;
        }
    }

    public bool HasUnsavedChanges
    {
        get
        {
            if (_projectData == null)
                return false;

            // Compare current UI values against effective values from ProjectData
            var effective = _projectData.Config.GetEffective();

            // Build current state from UI
            var currentType = IsProgrammableBlock ? ProjectType.ProgrammableBlock : ProjectType.Mod;
            var currentInteractive = ToInteractiveMode(MainConfig.Interactive?.Value);
            var currentOutput = ToCanonicalPath(MainConfig.OutputPath);
            var currentBinaryPath = ToCanonicalPath(MainConfig.BinaryPath);
            var currentMinify = ToMinifierLevel(MainConfig.Minify?.Value);
            var currentMinifyExtra = ToMinifierExtraOptions(MainConfig.MinifyExtraOptions?.Value);
            var currentTrace = ToBool(MainConfig.Trace?.Value);
            var currentIgnores = ToStringArray(MainConfig.Ignores);
            var currentNamespaces = ToStringArray(MainConfig.Namespaces);

            // Compare each value
            if (currentType != effective.Type) return true;
            if (currentInteractive != effective.Interactive) return true;
            if (!ProjectConfig.ComparePaths(currentOutput, effective.Output)) return true;
            if (!ProjectConfig.ComparePaths(currentBinaryPath, effective.BinaryPath)) return true;
            if (currentMinify != effective.Minify) return true;
            if (currentMinifyExtra != effective.MinifyExtraOptions) return true;
            if (currentTrace != effective.Trace) return true;
            if (!ProjectConfig.CompareIgnores(currentIgnores, effective.Ignores)) return true;
            if (!ProjectConfig.CompareNamespaces(currentNamespaces, effective.Namespaces)) return true;

            return false;
        }
    }

    public string? DefaultOutputPath => null; // TODO: Calculate from ProjectData if needed

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
                    var se = new SpaceEngineers();
                    _defaultBinaryPath = se.GetInstallPath("Bin64");
                }
                catch
                {
                    // Failed to find SE installation
                    _defaultBinaryPath = null;
                }
            }
            else
                _defaultBinaryPath = null;

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

    async Task NormalizeConfigurationAsync()
    {
        // Show confirmation dialog
        var result = await _dialogShell.ShowOverlayAsync(new ConfirmationMessage
        {
            Title = "Normalize Configuration?",
            Message = "This will reorganize your configuration files to the standard layout:\n\n" + "• Output, Binary Path, and Interactive → mdk.local.ini\n" + "• All other settings → mdk.ini\n\n" + "Backup files will be created before making changes.\n\n" + "Continue?",
            OkText = "Fix it",
            CancelText = "Cancel"
        });

        if (!result)
            return;

        try
        {
            var migratedData = await _projectService.NormalizeConfigurationAsync(_projectData!);
            // Save the normalized configuration
            await _projectService.SaveProjectDataAsync(migratedData);

            _shell.ShowToast("Configuration normalized successfully");

            _onClose(true); // Close the dialog after normalization
        }
        catch (Exception ex)
        {
            _logger.Error($"Configuration normalization failed for {ProjectName}", ex);
            await _dialogShell.ShowOverlayAsync(new InformationMessage
            {
                Title = "Normalization Failed",
                Message = $"Failed to normalize configuration:\n\n{ex.Message}\n\n" + "Your original files have not been modified.",
                OkText = "OK"
            });
        }
    }

    async void LoadConfiguration()
    {
        try
        {
            await Task.Yield(); // Ensure this runs asynchronously to avoid blocking the UI on load
            await LoadConfigurationAsync();
        }
        catch (Exception e)
        {
            _logger.Error($"Failed to load configuration for project: {ProjectName}", e);
            await _dialogShell.ShowOverlayAsync(new InformationMessage
            {
                Title = "Load Failed",
                Message = "Failed to load project configuration.\n\nPlease check the configuration files for errors.",
                OkText = "OK"
            });
            _onClose(false); // Close the dialog if we can't load the configuration
        }
    }

    async Task LoadConfigurationAsync()
    {
        _projectData = await _projectService.LoadProjectDataAsync(_projectPath);
        OnPropertyChanged(nameof(IsProgrammableBlock));
        OnPropertyChanged(nameof(IsStandardConfiguration));

        if (_projectData == null)
            return;

        // For standard configurations, load effective values into MainConfig
        // For non-standard configurations, this method won't be used (UI shows message instead)
        var effective = _projectData.Config.GetEffective();

        // Load all effective values into MainConfig (single plane editor)
        MainConfig.Interactive = effective.Interactive.HasValue
            ? ConfigurationSectionViewModel.InteractiveOptionsList.FirstOrDefault(o => o.Value.Equals(effective.Interactive.ToString(), StringComparison.OrdinalIgnoreCase))
            : ConfigurationSectionViewModel.InteractiveOptionsList[0]; // Default: OpenHub
        MainConfig.OutputPath = effective.Output?.Value ?? "auto";
        MainConfig.BinaryPath = effective.BinaryPath?.Value ?? "auto";
        MainConfig.Minify = effective.Minify.HasValue
            ? ConfigurationSectionViewModel.MinifyOptionsList.FirstOrDefault(o => o.Value.Equals(effective.Minify.ToString(), StringComparison.OrdinalIgnoreCase))
            : ConfigurationSectionViewModel.MinifyOptionsList[0]; // Default: none
        MainConfig.MinifyExtraOptions = effective.MinifyExtraOptions.HasValue
            ? ConfigurationSectionViewModel.MinifyExtraOptionsList.FirstOrDefault(o => o.Value.Equals(effective.MinifyExtraOptions.ToString(), StringComparison.OrdinalIgnoreCase))
            : ConfigurationSectionViewModel.MinifyExtraOptionsList[0]; // Default: none
        MainConfig.Trace = effective.Trace.HasValue
            ? ConfigurationSectionViewModel.TraceOptionsList.FirstOrDefault(o => o.Value == (effective.Trace.Value ? "true" : "false"))
            : ConfigurationSectionViewModel.TraceOptionsList[0]; // Default: false
        MainConfig.Ignores = effective.Ignores.HasValue ? string.Join(",", effective.Ignores.Value) : string.Empty;
        MainConfig.Namespaces = effective.Namespaces.HasValue ? string.Join(",", effective.Namespaces.Value) : "IngameScript";

        // LocalConfig is no longer used in simple editor mode (we don't need it)
    }

    async Task Save()
    {
        try
        {
            if (_projectData == null)
                throw new InvalidOperationException("Configuration not loaded");

            // Predetermined layer assignment:
            // Main layer: Type, Namespaces, Ignores, Minify, MinifyExtraOptions, Trace
            // Local layer: Output, BinaryPath, Interactive

            var mainLayer = new ProjectConfigLayer
            {
                Type = IsProgrammableBlock ? ProjectType.ProgrammableBlock : ProjectType.Mod,
                Interactive = null, // Goes to Local
                Output = null, // Goes to Local
                BinaryPath = null, // Goes to Local
                Minify = ToMinifierLevel(MainConfig.Minify?.Value),
                MinifyExtraOptions = ToMinifierExtraOptions(MainConfig.MinifyExtraOptions?.Value),
                Trace = ToBool(MainConfig.Trace?.Value),
                Ignores = ToStringArray(MainConfig.Ignores),
                Namespaces = ToStringArray(MainConfig.Namespaces)
            };

            var localLayer = new ProjectConfigLayer
            {
                Type = null, // Never in local
                Interactive = ToInteractiveMode(MainConfig.Interactive?.Value), // From MainConfig in simple mode
                Output = ToCanonicalPath(MainConfig.OutputPath), // From MainConfig in simple mode
                BinaryPath = ToCanonicalPath(MainConfig.BinaryPath), // From MainConfig in simple mode
                Minify = null, // Never in local
                MinifyExtraOptions = null, // Never in local
                Trace = null, // Never in local
                Ignores = null, // Never in local
                Namespaces = null // Never in local
            };

            // Create updated ProjectData
            var updatedProjectData = new ProjectData
            {
                Name = _projectData.Name,
                MainIni = _projectData.MainIni,
                LocalIni = _projectData.LocalIni,
                MainIniPath = _projectData.MainIniPath,
                LocalIniPath = _projectData.LocalIniPath,
                ProjectPath = _projectData.ProjectPath,
                Config = new ProjectConfig
                {
                    Default = _projectData.Config.Default,
                    Main = mainLayer,
                    Local = localLayer
                }
            };

            // Save using the new system
            await _projectService.SaveProjectDataAsync(updatedProjectData);

            _onClose(true); // Saved
        }
        catch (UnauthorizedAccessException ex)
        {
            await _dialogShell.ShowOverlayAsync(new ConfirmationMessage
            {
                Title = "Permission Denied",
                Message = $"Cannot save configuration file. Access is denied.\n\nPlease check file permissions and try again.\n\nDetails: {ex.Message}",
                OkText = "OK",
                CancelText = ""
            });
        }
        catch (IOException ex)
        {
            await _dialogShell.ShowOverlayAsync(new ConfirmationMessage
            {
                Title = "Save Failed",
                Message = $"Cannot save configuration file. The disk may be full or the file may be in use.\n\nDetails: {ex.Message}",
                OkText = "OK",
                CancelText = ""
            });
        }
    }

    void NotifyDirtyStateChanged() => _onDirtyStateChanged?.Invoke();

    void Cancel() => _onClose(false); // Cancelled

    void ClearLocalInteractive() => LocalConfig.Interactive = null;

    void ClearLocalOutputPath() => LocalConfig.OutputPath = string.Empty;

    void ClearLocalBinaryPath() => LocalConfig.BinaryPath = string.Empty;

    void ClearLocalMinify() => LocalConfig.Minify = null;

    void ClearLocalMinifyExtraOptions() => LocalConfig.MinifyExtraOptions = null;

    void ClearLocalTrace() => LocalConfig.Trace = null;

    void ClearLocalIgnores() => LocalConfig.Ignores = string.Empty;

    void ClearLocalNamespaces() => LocalConfig.Namespaces = string.Empty;

    InteractiveMode? ToInteractiveMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return Enum.TryParse<InteractiveMode>(value, true, out var result) ? result : null;
    }

    MinifierLevel? ToMinifierLevel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return Enum.TryParse<MinifierLevel>(value, true, out var result) ? result : null;
    }

    MinifierExtraOptions? ToMinifierExtraOptions(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return Enum.TryParse<MinifierExtraOptions>(value, true, out var result) ? result : null;
    }

    bool? ToBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.ToLowerInvariant() switch
        {
            "true" => true,
            "false" => false,
            _ => null
        };
    }

    ImmutableArray<string>? ToStringArray(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var items = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToImmutableArray();

        return items.IsEmpty ? null : items;
    }

    CanonicalPath? ToCanonicalPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();

        // "auto" means null (auto-detect)
        if (value.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return null;

        // Real path
        try
        {
            return new CanonicalPath(value);
        }
        catch
        {
            // Invalid path, treat as null
            return null;
        }
    }

    void OpenGlobalSettings()
    {
        var viewModel = App.Container.Resolve<GlobalSettingsViewModel>();
        _shell.AddOverlay(viewModel);
    }
}