using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Features.Storage;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Actions.Items;

/// <summary>
///     Action displaying project information and providing quick access to project operations.
/// </summary>
[Singleton]
[ViewModelFor<ProjectInfoActionView>]
public class ProjectInfoAction : ActionItem
{
    readonly ProjectActionsViewModel _actionsViewModel;
    readonly IFileStorageService _fileStorage;
    readonly IProjectService _projectService;
    readonly IShell _shell;
    string? _configurationWarning;
    string? _deploymentError;
    bool _isDeployed;
    bool _isLoading = true;
    DateTimeOffset? _lastChanged;
    string? _lastChangedError;
    DateTimeOffset? _lastDeployed;
    string? _outputPath;
    int? _scriptSizeCharacters;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectInfoAction"/> class.
    /// </summary>
    /// <param name="projectService">The service for managing projects.</param>
    /// <param name="shell">The shell interface for UI interactions.</param>
    /// <param name="actionsViewModel">The view model for project actions.</param>
    /// <param name="fileStorage">The file storage service.</param>
    public ProjectInfoAction(IProjectService projectService, IShell shell, ProjectActionsViewModel actionsViewModel, IFileStorageService fileStorage)
    {
        _projectService = projectService;
        _shell = shell;
        _actionsViewModel = actionsViewModel;
        _fileStorage = fileStorage;

        OpenProjectFolderCommand = new RelayCommand(OpenProjectFolder, CanOpenProjectFolder);
        OpenOutputFolderCommand = new RelayCommand(OpenOutputFolder, CanOpenOutputFolder);
        OpenInIdeCommand = new RelayCommand(OpenInIde, CanOpenInIde);
        CopyScriptCommand = new AsyncRelayCommand(CopyScriptAsync, CanCopyScript);
        ShowOptionsCommand = new RelayCommand(ShowOptions, CanShowOptions);
    }

    bool _isScript;
    string _projectTypeName = string.Empty;

    /// <summary>
    ///     Gets the single selected project, or null if zero or multiple projects are selected.
    ///     Compatibility property for single-select actions.
    /// </summary>
    public ProjectModel? Project => SelectedProjects.Length == 1 ? SelectedProjects[0] : null;

    /// <summary>
    ///     Gets whether the selected project is a script (programmable block).
    /// </summary>
    public bool IsScript
    {
        get => _isScript;
        private set => SetProperty(ref _isScript, value);
    }

    /// <summary>
    ///     Gets the display name for the project type.
    /// </summary>
    public string ProjectTypeName
    {
        get => _projectTypeName;
        private set => SetProperty(ref _projectTypeName, value);
    }

    /// <summary>
    ///     Gets or sets whether project data is currently loading.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    ///     Gets or sets the last time the project file was modified.
    /// </summary>
    public DateTimeOffset? LastChanged
    {
        get => _lastChanged;
        private set => SetProperty(ref _lastChanged, value);
    }

    /// <summary>
    ///     Gets or sets any error that occurred while reading the last changed time.
    /// </summary>
    public string? LastChangedError
    {
        get => _lastChangedError;
        private set => SetProperty(ref _lastChangedError, value);
    }

    /// <summary>
    ///     Gets or sets whether the project has been deployed to the output folder.
    /// </summary>
    public bool IsDeployed
    {
        get => _isDeployed;
        private set
        {
            if (SetProperty(ref _isDeployed, value))
            {
                ((RelayCommand)OpenOutputFolderCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)CopyScriptCommand).NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    ///     Gets or sets any error that occurred while checking deployment status.
    /// </summary>
    public string? DeploymentError
    {
        get => _deploymentError;
        private set => SetProperty(ref _deploymentError, value);
    }

    /// <summary>
    ///     Gets or sets the last time the project was deployed.
    /// </summary>
    public DateTimeOffset? LastDeployed
    {
        get => _lastDeployed;
        private set => SetProperty(ref _lastDeployed, value);
    }

    /// <summary>
    ///     Gets or sets the size of the deployed script in characters.
    /// </summary>
    public int? ScriptSizeCharacters
    {
        get => _scriptSizeCharacters;
        private set
        {
            if (SetProperty(ref _scriptSizeCharacters, value))
                IsScriptTooLarge = value is > 100_000;
        }
    }

    /// <summary>
    ///     Gets or sets any configuration warning for the project.
    /// </summary>
    public string? ConfigurationWarning
    {
        get => _configurationWarning;
        private set => SetProperty(ref _configurationWarning, value);
    }

    bool _isScriptTooLarge;

    /// <summary>
    ///     Gets whether the script exceeds the Space Engineers character limit.
    /// </summary>
    public bool IsScriptTooLarge
    {
        get => _isScriptTooLarge;
        private set => SetProperty(ref _isScriptTooLarge, value);
    }

    /// <summary>
    ///     Gets the command to open the project folder in explorer.
    /// </summary>
    public ICommand OpenProjectFolderCommand { get; }
    
    /// <summary>
    ///     Gets the command to open the output/deployment folder.
    /// </summary>
    public ICommand OpenOutputFolderCommand { get; }
    
    /// <summary>
    ///     Gets the command to open the project in an IDE.
    /// </summary>
    public ICommand OpenInIdeCommand { get; }
    
    /// <summary>
    ///     Gets the command to copy the script to clipboard.
    /// </summary>
    public ICommand CopyScriptCommand { get; }
    
    /// <summary>
    ///     Gets the command to show project options.
    /// </summary>
    public ICommand ShowOptionsCommand { get; }

    /// <summary>
    ///     Gets the action category.
    /// </summary>
    public override string Category => "Project";

    /// <summary>
    ///     Called when the selected projects change to update project-specific UI elements.
    /// </summary>
    protected override void OnSelectedProjectsChanged()
    {
        base.OnSelectedProjectsChanged();

        // Update computed properties
        OnPropertyChanged(nameof(Project));
        
        var project = Project;
        IsScript = project?.Type == ProjectType.ProgrammableBlock;
        ProjectTypeName = project?.Type == ProjectType.ProgrammableBlock 
            ? "Programmable Block Script" 
            : "Mod";

        // Reload project data for new project
        if (project != null)
            _ = LoadProjectDataAsync(_projectService);
    }

    /// <summary>
    ///     Determines whether this action should be shown in the UI.
    /// </summary>
    public override bool ShouldShow()
    {
        // Only show for single selection
        if (!base.ShouldShow())
            return false;
            
        return !_projectService.State.SelectedProject.IsEmpty();
    }

    bool CanOpenProjectFolder() => Project is not null && File.Exists(Project.ProjectPath.Value);

    void OpenProjectFolder()
    {
        if (!CanOpenProjectFolder())
            return;

        _projectService.OpenProjectFolder(Project!.ProjectPath);
    }

    bool CanOpenOutputFolder() => IsDeployed && !string.IsNullOrEmpty(_outputPath) && Directory.Exists(_outputPath);

    void OpenOutputFolder()
    {
        if (!CanOpenOutputFolder())
            return;

        _projectService.OpenOutputFolderAsync(Project!.ProjectPath);
    }

    bool CanOpenInIde() => Project is not null && File.Exists(Project.ProjectPath.Value);

    void OpenInIde()
    {
        if (!CanOpenInIde())
            return;

        if (_projectService.OpenProjectInIde(Project!.ProjectPath))
            _shell.ShowToast($"Opening {Project.Name} in IDE...");
    }

    bool CanCopyScript() => IsScript && IsDeployed && !string.IsNullOrEmpty(_outputPath);

    async Task CopyScriptAsync()
    {
        if (!CanCopyScript())
            return;

        try
        {
            var scriptFile = Path.Combine(_outputPath!, "Script.cs");

            if (File.Exists(scriptFile))
            {
                var content = await File.ReadAllTextAsync(scriptFile);

                // Use TopLevel to get clipboard
                var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    ? TopLevel.GetTopLevel(desktop.MainWindow)
                    : null;

                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(content);
                    _shell.ShowToast($"Script copied ({content.Length:N0} characters)");
                }
            }
        }
        catch (Exception ex)
        {
            await _shell.ShowOverlayAsync(new ConfirmationMessage
            {
                Title = "Copy Failed",
                Message = $"Failed to copy script to clipboard: {ex.Message}",
                OkText = "OK",
                CancelText = "Close"
            });
        }
    }

    bool CanShowOptions() => Project is not null && File.Exists(Project.ProjectPath.Value);

    void ShowOptions()
    {
        if (!CanShowOptions())
            return;

        if (Project!.ProjectPath.IsEmpty())
            return;

        _actionsViewModel.ShowOptionsDrawer(Project.ProjectPath.Value!);
    }

    async Task LoadProjectDataAsync(IProjectService projectService)
    {
        try
        {
            // Run I/O operations on background thread
            var result = await Task.Run(async () =>
            {
                DateTimeOffset? lastChanged = null;
                string? lastChangedError = null;
                var isDeployed = false;
                string? deploymentError = null;
                DateTimeOffset? lastDeployed = null;
                int? scriptSize = null;
                string? outputPath = null;
                string? configWarning = null;

                var project = Project;
                if (project == null)
                    return (lastChanged, lastChangedError, isDeployed, deploymentError, lastDeployed, scriptSize, outputPath, configWarning);
                    
                var projectPath = project.ProjectPath;
                if (string.IsNullOrEmpty(projectPath.Value))
                    return (lastChanged, lastChangedError, isDeployed, deploymentError, lastDeployed, scriptSize, outputPath, configWarning);

                // Load last changed time from project file
                try
                {
                    if (File.Exists(projectPath.Value))
                        lastChanged = File.GetLastWriteTime(projectPath.Value);
                }
                catch (UnauthorizedAccessException)
                {
                    lastChangedError = "Permission denied";
                }
                catch (IOException ex)
                {
                    lastChangedError = $"Error: {ex.Message}";
                }

                // Load configuration and check deployment
                var projectData = await projectService.LoadProjectDataAsync(projectPath);
                configWarning = null;

                if (projectData != null)
                {
                    var config = projectData.Config.GetEffective();
                    var outputPathValue = config.Output?.Value;

                    // Resolve "auto" to actual path
                    if (string.Equals(outputPathValue, "auto", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(outputPathValue))
                    {
                        var projectName = Path.GetFileNameWithoutExtension(projectPath.Value);
                        var seDataPath = config.Type == ProjectType.ProgrammableBlock
                            ? Path.Combine(_fileStorage.GetSpaceEngineersDataPath(), "IngameScripts", "local", projectName)
                            : config.Type == ProjectType.Mod
                                ? Path.Combine(_fileStorage.GetSpaceEngineersDataPath(), "Mods", projectName)
                                : null;
                        outputPath = seDataPath;
                    }
                    else if (!string.IsNullOrEmpty(outputPathValue))
                    {
                        // Custom paths should include project name subfolder
                        var projectName = Path.GetFileNameWithoutExtension(projectPath.Value);
                        outputPath = Path.Combine(outputPathValue, projectName);
                    }

                    if (!string.IsNullOrEmpty(outputPath) && Directory.Exists(outputPath))
                    {
                        isDeployed = true;

                        // Get the most recent file write time in the output directory
                        try
                        {
                            var files = Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories);
                            if (files.Length > 0)
                            {
                                var mostRecent = files.Max(File.GetLastWriteTime);
                                lastDeployed = mostRecent;
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            deploymentError = "Permission denied accessing deployment folder";
                            isDeployed = false;
                        }
                        catch (DirectoryNotFoundException)
                        {
                            // Directory was deleted between Exists check and GetFiles
                            isDeployed = false;
                        }
                        catch (IOException ex)
                        {
                            deploymentError = $"Error reading deployment folder: {ex.Message}";
                            isDeployed = false;
                        }
                    }

                    // For scripts, try to load the deployed script size
                    if (IsScript && !string.IsNullOrEmpty(outputPath))
                    {
                        try
                        {
                            var scriptFile = Path.Combine(outputPath, "Script.cs");
                            if (File.Exists(scriptFile))
                            {
                                var content = File.ReadAllText(scriptFile);
                                scriptSize = content.Length;
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Can't read script file - deployment status already set above
                        }
                        catch (FileNotFoundException)
                        {
                            // File was deleted - ignore
                        }
                        catch (IOException)
                        {
                            // Other I/O errors - ignore (deployment status already set)
                        }
                    }
                }

                return (lastChanged, lastChangedError, isDeployed, deploymentError, lastDeployed, scriptSize, outputPath, configWarning);
            });

            // Update properties on UI thread
            LastChanged = result.lastChanged;
            LastChangedError = result.lastChangedError;
            
            var outputPathChanged = _outputPath != result.outputPath;
            _outputPath = result.outputPath;
            
            IsDeployed = result.isDeployed;
            DeploymentError = result.deploymentError;
            LastDeployed = result.lastDeployed;
            ScriptSizeCharacters = result.scriptSize;
            ConfigurationWarning = result.configWarning;
            
            // Notify command if output path changed
            if (outputPathChanged)
                ((AsyncRelayCommand)CopyScriptCommand).NotifyCanExecuteChanged();
        }
        finally
        {
            IsLoading = false;
        }
    }
}
