using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Actions;

public class ProjectInfoAction : ActionItem
{
    readonly IProjectService _projectService;
    readonly ICommonDialogs _commonDialogs;
    readonly IShell _shell;
    readonly ProjectActionsViewModel _actionsViewModel;
    DateTimeOffset? _lastChanged;
    bool _isDeployed;
    DateTimeOffset? _lastDeployed;
    int? _scriptSizeCharacters;
    bool _isLoading = true;
    string? _outputPath;

    public ProjectInfoAction(ProjectModel project, IProjectService projectService, ICommonDialogs commonDialogs, IShell shell, ProjectActionsViewModel actionsViewModel)
    {
        Project = project;
        _projectService = projectService;
        _commonDialogs = commonDialogs;
        _shell = shell;
        _actionsViewModel = actionsViewModel;
        
        OpenProjectFolderCommand = new RelayCommand(OpenProjectFolder, CanOpenProjectFolder);
        OpenOutputFolderCommand = new RelayCommand(OpenOutputFolder, CanOpenOutputFolder);
        OpenInIdeCommand = new RelayCommand(OpenInIde, CanOpenInIde);
        CopyScriptCommand = new AsyncRelayCommand(CopyScriptAsync, CanCopyScript);
        ShowOptionsCommand = new RelayCommand(ShowOptions, CanShowOptions);
        
        // Load data asynchronously
        _ = LoadProjectDataAsync(projectService);
    }

    public ProjectModel Project { get; }

    public bool IsScript => Project.Type == ProjectType.IngameScript;

    public string ProjectTypeName => Project.Type == ProjectType.IngameScript 
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

    public bool IsScriptTooLarge => ScriptSizeCharacters.HasValue && ScriptSizeCharacters.Value > 100_000;

    public ICommand OpenProjectFolderCommand { get; }
    public ICommand OpenOutputFolderCommand { get; }
    public ICommand OpenInIdeCommand { get; }
    public ICommand CopyScriptCommand { get; }
    public ICommand ShowOptionsCommand { get; }

    public override string? Category => "Project";

    public override bool ShouldShow(ProjectListItem? selectedProject, bool canMakeScript, bool canMakeMod)
    {
        return selectedProject is ProjectModel;
    }

    bool CanOpenProjectFolder()
    {
        return File.Exists(Project.ProjectPath);
    }

    void OpenProjectFolder()
    {
        if (!CanOpenProjectFolder())
            return;

        var folder = Path.GetDirectoryName(Project.ProjectPath);
        if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }
    }

    bool CanOpenOutputFolder()
    {
        return IsDeployed && !string.IsNullOrEmpty(_outputPath) && Directory.Exists(_outputPath);
    }

    void OpenOutputFolder()
    {
        if (!CanOpenOutputFolder())
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = _outputPath!,
            UseShellExecute = true
        });
    }

    bool CanOpenInIde()
    {
        return File.Exists(Project.ProjectPath);
    }

    void OpenInIde()
    {
        if (!CanOpenInIde())
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Project.ProjectPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            // Silently fail - user's system might not have a default handler for .csproj
            Debug.WriteLine($"Failed to open project in IDE: {ex.Message}");
        }
    }

    bool CanCopyScript()
    {
        return IsScript && IsDeployed && !string.IsNullOrEmpty(_outputPath);
    }

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
                var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                    ? Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow)
                    : null;
                
                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(content);
                    _commonDialogs.ShowToast($"Script copied ({content.Length:N0} characters)");
                }
            }
        }
        catch (Exception ex)
        {
            await _commonDialogs.ShowAsync(new ConfirmationMessage
            {
                Title = "Copy Failed",
                Message = $"Failed to copy script to clipboard: {ex.Message}",
                OkText = "OK",
                CancelText = "Close"
            });
        }
    }

    bool CanShowOptions() => File.Exists(Project.ProjectPath);

    void ShowOptions()
    {
        if (!CanShowOptions())
            return;

        _actionsViewModel.ShowOptionsDrawer(Project.ProjectPath);
    }

    async Task LoadProjectDataAsync(IProjectService projectService)
    {
        try
        {
            // Run I/O operations on background thread
            var result = await Task.Run(() =>
            {
                DateTimeOffset? lastChanged = null;
                bool isDeployed = false;
                DateTimeOffset? lastDeployed = null;
                int? scriptSize = null;
                string? outputPath = null;

                // Load last changed time from project file
                if (File.Exists(Project.ProjectPath))
                {
                    lastChanged = File.GetLastWriteTime(Project.ProjectPath);
                }

                // Load configuration and check deployment
                var config = projectService.LoadConfiguration(Project.ProjectPath);
                if (config != null)
                {
                    outputPath = config.GetResolvedOutputPath();
                    
                    if (!string.IsNullOrEmpty(outputPath) && Directory.Exists(outputPath))
                    {
                        isDeployed = true;
                        
                        // Get the most recent file write time in the output directory
                        try
                        {
                            var files = Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories);
                            if (files.Length > 0)
                            {
                                var mostRecent = files.Max(f => File.GetLastWriteTime(f));
                                lastDeployed = mostRecent;
                            }
                        }
                        catch
                        {
                            // Ignore errors reading output directory
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
                        catch
                        {
                            // Ignore errors reading script file
                        }
                    }
                }

                return (lastChanged, isDeployed, lastDeployed, scriptSize, outputPath);
            });

            // Update properties on UI thread
            LastChanged = result.lastChanged;
            _outputPath = result.outputPath;
            IsDeployed = result.isDeployed;
            LastDeployed = result.lastDeployed;
            ScriptSizeCharacters = result.scriptSize;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
