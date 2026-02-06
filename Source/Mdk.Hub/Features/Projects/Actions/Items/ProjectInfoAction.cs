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
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Actions.Items;

[Singleton]
[ViewModelFor<ProjectInfoActionView>]
public class ProjectInfoAction : ActionItem
{
    readonly ProjectActionsViewModel _actionsViewModel;
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

    public ProjectInfoAction(IProjectService projectService, IShell shell, ProjectActionsViewModel actionsViewModel)
    {
        _projectService = projectService;
        _shell = shell;
        _actionsViewModel = actionsViewModel;

        OpenProjectFolderCommand = new RelayCommand(OpenProjectFolder, CanOpenProjectFolder);
        OpenOutputFolderCommand = new RelayCommand(OpenOutputFolder, CanOpenOutputFolder);
        OpenInIdeCommand = new RelayCommand(OpenInIde, CanOpenInIde);
        CopyScriptCommand = new AsyncRelayCommand(CopyScriptAsync, CanCopyScript);
        ShowOptionsCommand = new RelayCommand(ShowOptions, CanShowOptions);
    }

    public bool IsScript => Project?.Type == ProjectType.ProgrammableBlock;

    public string ProjectTypeName => Project?.Type == ProjectType.ProgrammableBlock
        ? "Programmable Block Script"
        : "Mod";

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public DateTimeOffset? LastChanged
    {
        get => _lastChanged;
        private set => SetProperty(ref _lastChanged, value);
    }

    public string? LastChangedError
    {
        get => _lastChangedError;
        private set => SetProperty(ref _lastChangedError, value);
    }

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

    public string? DeploymentError
    {
        get => _deploymentError;
        private set => SetProperty(ref _deploymentError, value);
    }

    public DateTimeOffset? LastDeployed
    {
        get => _lastDeployed;
        private set => SetProperty(ref _lastDeployed, value);
    }

    public int? ScriptSizeCharacters
    {
        get => _scriptSizeCharacters;
        private set
        {
            if (SetProperty(ref _scriptSizeCharacters, value))
                OnPropertyChanged(nameof(IsScriptTooLarge));
        }
    }

    public string? ConfigurationWarning
    {
        get => _configurationWarning;
        private set => SetProperty(ref _configurationWarning, value);
    }

    public bool IsScriptTooLarge => ScriptSizeCharacters is > 100_000;

    public ICommand OpenProjectFolderCommand { get; }
    public ICommand OpenOutputFolderCommand { get; }
    public ICommand OpenInIdeCommand { get; }
    public ICommand CopyScriptCommand { get; }
    public ICommand ShowOptionsCommand { get; }

    public override string Category => "Project";

    protected override void OnSelectedProjectChanged()
    {
        base.OnSelectedProjectChanged();

        // Update computed properties
        OnPropertyChanged(nameof(IsScript));
        OnPropertyChanged(nameof(ProjectTypeName));

        // Reload project data for new project
        if (Project != null)
            _ = LoadProjectDataAsync(_projectService);
    }

    public override bool ShouldShow() => !_projectService.State.SelectedProject.IsEmpty();

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

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Project?.ProjectPath.Value,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            // Silently fail - user's system might not have a default handler for .csproj
            Debug.WriteLine($"Failed to open project in IDE: {ex.Message}");
        }
    }

    bool CanCopyScript() => IsScript && IsDeployed && !string.IsNullOrEmpty(_outputPath);

    async Task CopyScriptAsync()
    {
        Debug.WriteLine($"CopyScriptAsync called. IsScript={IsScript}, IsDeployed={IsDeployed}, _outputPath={_outputPath}");

        if (!CanCopyScript())
        {
            Debug.WriteLine("CanCopyScript returned false, exiting early");
            return;
        }

        try
        {
            var scriptFile = Path.Combine(_outputPath!, "Script.cs");
            Debug.WriteLine($"Looking for script at: {scriptFile}");

            if (File.Exists(scriptFile))
            {
                var content = await File.ReadAllTextAsync(scriptFile);
                Debug.WriteLine($"Script loaded, length={content.Length}");

                // Use TopLevel to get clipboard
                var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    ? TopLevel.GetTopLevel(desktop.MainWindow)
                    : null;

                Debug.WriteLine($"TopLevel: {topLevel != null}, Clipboard: {topLevel?.Clipboard != null}");

                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(content);
                    Debug.WriteLine("Clipboard set successfully");
                    _shell.ShowToast($"Script copied ({content.Length:N0} characters)");
                }
            }
            else
                Debug.WriteLine("Script file does not exist");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception: {ex}");
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
                        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        outputPath = config.Type == ProjectType.ProgrammableBlock
                            ? Path.Combine(appData, "SpaceEngineers", "IngameScripts", "local", projectName)
                            : config.Type == ProjectType.Mod
                                ? Path.Combine(appData, "SpaceEngineers", "Mods", projectName)
                                : null;
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
            _outputPath = result.outputPath;
            IsDeployed = result.isDeployed;
            DeploymentError = result.deploymentError;
            LastDeployed = result.lastDeployed;
            ScriptSizeCharacters = result.scriptSize;
            ConfigurationWarning = result.configWarning;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
