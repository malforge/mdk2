using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Mdk.Hub.Features.CommonDialogs;
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
    readonly Action<bool> _onClose; // bool parameter: true if saved, false if cancelled
    readonly Action? _onDirtyStateChanged;
    readonly RelayCommand _openGlobalSettingsCommand;
    readonly CanonicalPath _projectPath;
    readonly IProjectService _projectService;

    readonly AsyncRelayCommand _saveCommand;
    readonly IShell _shell;
    ProjectData? _projectData;
    string? _defaultBinaryPath;
    bool _defaultBinaryPathLoaded;

    public ProjectOptionsViewModel(string projectPath, IProjectService projectService, IShell dialogShell, IShell shell, Action<bool> onClose, Action? onDirtyStateChanged = null)
    {
        _projectPath = new CanonicalPath(projectPath);
        _projectService = projectService;
        _dialogShell = dialogShell;
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

    public bool HasUnsavedChanges
    {
        get
        {
            if (_projectData == null)
                return false;

            // Check if Main layer has changed
            var currentMain = new ProjectConfigLayer
            {
                Type = IsProgrammableBlock ? ProjectType.ProgrammableBlock : ProjectType.Mod,
                Interactive = ToInteractiveMode(MainConfig.Interactive?.Value),
                Output = ToCanonicalPath(MainConfig.OutputPath),
                BinaryPath = ToCanonicalPath(MainConfig.BinaryPath),
                Minify = ToMinifierLevel(MainConfig.Minify?.Value),
                MinifyExtraOptions = ToMinifierExtraOptions(MainConfig.MinifyExtraOptions?.Value),
                Trace = ToBool(MainConfig.Trace?.Value),
                Ignores = ToStringArray(MainConfig.Ignores),
                Namespaces = ToStringArray(MainConfig.Namespaces)
            };

            // Check if Local layer has changed
            var currentLocal = new ProjectConfigLayer
            {
                Type = null,
                Interactive = ToInteractiveMode(LocalConfig.Interactive?.Value),
                Output = ToCanonicalPath(LocalConfig.OutputPath),
                BinaryPath = ToCanonicalPath(LocalConfig.BinaryPath),
                Minify = ToMinifierLevel(LocalConfig.Minify?.Value),
                MinifyExtraOptions = ToMinifierExtraOptions(LocalConfig.MinifyExtraOptions?.Value),
                Trace = ToBool(LocalConfig.Trace?.Value),
                Ignores = ToStringArray(LocalConfig.Ignores),
                Namespaces = ToStringArray(LocalConfig.Namespaces)
            };

            bool mainChanged = !(_projectData.Config.Main?.Equals(currentMain) ?? false);
            bool localChanged = !(_projectData.Config.Local?.Equals(currentLocal) ?? false);

            return mainChanged || localChanged;
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

    void LoadConfiguration()
    {
        _projectData = _projectService.LoadProjectData(_projectPath);
        OnPropertyChanged(nameof(IsProgrammableBlock));

        // Set defaults for Main (these will be overwritten if data exists)
        MainConfig.Interactive = ConfigurationSectionViewModel.InteractiveOptionsList[0]; // "OpenHub"
        MainConfig.Minify = ConfigurationSectionViewModel.MinifyOptionsList[0]; // "none"
        MainConfig.MinifyExtraOptions = ConfigurationSectionViewModel.MinifyExtraOptionsList[0]; // "none"
        MainConfig.Trace = ConfigurationSectionViewModel.TraceOptionsList[0]; // "false"
        MainConfig.Namespaces = "IngameScript";
        MainConfig.OutputPath = "auto";
        MainConfig.BinaryPath = "auto";

        if (_projectData == null)
            return;

        // Load Main from ProjectData
        if (_projectData.Config.Main != null)
        {
            var main = _projectData.Config.Main;
            MainConfig.Interactive = main.Interactive.HasValue 
                ? ConfigurationSectionViewModel.InteractiveOptionsList.FirstOrDefault(o => o.Value.Equals(main.Interactive.ToString(), StringComparison.OrdinalIgnoreCase))
                : MainConfig.Interactive;
            MainConfig.OutputPath = main.Output?.Value ?? "auto";
            MainConfig.BinaryPath = main.BinaryPath?.Value ?? "auto";
            MainConfig.Minify = main.Minify.HasValue
                ? ConfigurationSectionViewModel.MinifyOptionsList.FirstOrDefault(o => o.Value.Equals(main.Minify.ToString(), StringComparison.OrdinalIgnoreCase))
                : MainConfig.Minify;
            MainConfig.MinifyExtraOptions = main.MinifyExtraOptions.HasValue
                ? ConfigurationSectionViewModel.MinifyExtraOptionsList.FirstOrDefault(o => o.Value.Equals(main.MinifyExtraOptions.ToString(), StringComparison.OrdinalIgnoreCase))
                : MainConfig.MinifyExtraOptions;
            MainConfig.Trace = main.Trace.HasValue
                ? ConfigurationSectionViewModel.TraceOptionsList.FirstOrDefault(o => o.Value == (main.Trace.Value ? "true" : "false"))
                : MainConfig.Trace;
            MainConfig.Ignores = main.Ignores.HasValue ? string.Join(",", main.Ignores.Value) : string.Empty;
            MainConfig.Namespaces = main.Namespaces.HasValue ? string.Join(",", main.Namespaces.Value) : "IngameScript";
        }

        // Load Local from ProjectData
        if (_projectData.Config.Local != null)
        {
            var local = _projectData.Config.Local;
            LocalConfig.Interactive = local.Interactive.HasValue
                ? ConfigurationSectionViewModel.InteractiveOptionsList.FirstOrDefault(o => o.Value.Equals(local.Interactive.ToString(), StringComparison.OrdinalIgnoreCase))
                : null;
            LocalConfig.OutputPath = local.Output?.Value ?? string.Empty;
            LocalConfig.BinaryPath = local.BinaryPath?.Value ?? string.Empty;
            LocalConfig.Minify = local.Minify.HasValue
                ? ConfigurationSectionViewModel.MinifyOptionsList.FirstOrDefault(o => o.Value.Equals(local.Minify.ToString(), StringComparison.OrdinalIgnoreCase))
                : null;
            LocalConfig.MinifyExtraOptions = local.MinifyExtraOptions.HasValue
                ? ConfigurationSectionViewModel.MinifyExtraOptionsList.FirstOrDefault(o => o.Value.Equals(local.MinifyExtraOptions.ToString(), StringComparison.OrdinalIgnoreCase))
                : null;
            LocalConfig.Trace = local.Trace.HasValue
                ? ConfigurationSectionViewModel.TraceOptionsList.FirstOrDefault(o => o.Value == (local.Trace.Value ? "true" : "false"))
                : null;
            LocalConfig.Ignores = local.Ignores.HasValue ? string.Join(",", local.Ignores.Value) : string.Empty;
            LocalConfig.Namespaces = local.Namespaces.HasValue ? string.Join(",", local.Namespaces.Value) : string.Empty;
        }

        // If interactive is not set anywhere, default LocalConfig to "ShowNotification"
        if ((_projectData.Config.Main?.Interactive.HasValue != true) && (_projectData.Config.Local?.Interactive.HasValue != true))
            LocalConfig.Interactive = ConfigurationSectionViewModel.InteractiveOptionsList.FirstOrDefault(o => o.Value == "ShowNotification");
    }

    async Task Save()
    {
        try
        {
            if (_projectData == null)
                throw new InvalidOperationException("Configuration not loaded");

            // Build Main layer from UI
            var mainLayer = new ProjectConfigLayer
            {
                Type = IsProgrammableBlock ? ProjectType.ProgrammableBlock : ProjectType.Mod,
                Interactive = ToInteractiveMode(MainConfig.Interactive?.Value),
                Output = ToCanonicalPath(MainConfig.OutputPath),
                BinaryPath = ToCanonicalPath(MainConfig.BinaryPath),
                Minify = ToMinifierLevel(MainConfig.Minify?.Value),
                MinifyExtraOptions = ToMinifierExtraOptions(MainConfig.MinifyExtraOptions?.Value),
                Trace = ToBool(MainConfig.Trace?.Value),
                Ignores = ToStringArray(MainConfig.Ignores),
                Namespaces = ToStringArray(MainConfig.Namespaces)
            };

            // Build Local layer from UI (null means inherit from main)
            var localLayer = new ProjectConfigLayer
            {
                Type = null, // Type is always in main, never in local
                Interactive = ToInteractiveMode(LocalConfig.Interactive?.Value),
                Output = ToCanonicalPath(LocalConfig.OutputPath),
                BinaryPath = ToCanonicalPath(LocalConfig.BinaryPath),
                Minify = ToMinifierLevel(LocalConfig.Minify?.Value),
                MinifyExtraOptions = ToMinifierExtraOptions(LocalConfig.MinifyExtraOptions?.Value),
                Trace = ToBool(LocalConfig.Trace?.Value),
                Ignores = ToStringArray(LocalConfig.Ignores),
                Namespaces = ToStringArray(LocalConfig.Namespaces)
            };

            // Create updated ProjectData
            var updatedProjectData = new ProjectData
            {
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
            await _projectService.SaveProjectData(updatedProjectData);

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
        return Enum.TryParse<InteractiveMode>(value, ignoreCase: true, out var result) ? result : null;
    }

    MinifierLevel? ToMinifierLevel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return Enum.TryParse<MinifierLevel>(value, ignoreCase: true, out var result) ? result : null;
    }

    MinifierExtraOptions? ToMinifierExtraOptions(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return Enum.TryParse<MinifierExtraOptions>(value, ignoreCase: true, out var result) ? result : null;
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

