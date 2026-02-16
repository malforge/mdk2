using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Projects.NewProjectDialog;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Projects.Overview.Icons;
using Mdk.Hub.Features.Settings;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Features.Storage;
using Mdk.Hub.Framework;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects.Actions.Items;

/// <summary>
///     Action for creating new projects and adding existing projects to the Hub.
/// </summary>
[Singleton]
[ViewModelFor<ProjectManagementActionView>]
public class ProjectManagementAction : ActionItem
{
    readonly AsyncRelayCommand _addExistingCommand;
    readonly AsyncRelayCommand _createModCommand;
    readonly AsyncRelayCommand _createScriptCommand;
    readonly AsyncRelayCommand _createMixinCommand;
    readonly IFileStorageService _fileStorage;
    readonly IProjectService _projectService;
    readonly IShell _shell;

    bool _canMakeScript;
    bool _canMakeMod;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectManagementAction"/> class.
    /// </summary>
    /// <param name="shell">The shell interface for UI interactions.</param>
    /// <param name="projectService">The service for managing projects.</param>
    /// <param name="fileStorage">The file storage service.</param>
    public ProjectManagementAction(
        IShell shell,
        IProjectService projectService,
        IFileStorageService fileStorage)
    {
        _shell = shell;
        _projectService = projectService;
        _fileStorage = fileStorage;

        _createScriptCommand = new AsyncRelayCommand(CreateScriptAsync, () => CanMakeScript);
        _createModCommand = new AsyncRelayCommand(CreateModAsync, () => CanMakeMod);
        _createMixinCommand = new AsyncRelayCommand(CreateMixinAsync, () => true);
        _addExistingCommand = new AsyncRelayCommand(AddExistingProjectAsync);

        _projectService.StateChanged += OnProjectServiceStateChanged;
        
        // Initialize from current state
        UpdateCanMakeProperties();
    }

    /// <summary>
    ///     Gets whether a new Programmable Block Script can be created.
    /// </summary>
    public bool CanMakeScript
    {
        get => _canMakeScript;
        private set => SetProperty(ref _canMakeScript, value);
    }

    /// <summary>
    ///     Gets whether a new Mod can be created.
    /// </summary>
    public bool CanMakeMod
    {
        get => _canMakeMod;
        private set => SetProperty(ref _canMakeMod, value);
    }

    /// <summary>
    ///     Gets the command to create a new Programmable Block Script.
    /// </summary>
    public ICommand CreateScriptCommand => _createScriptCommand;

    /// <summary>
    ///     Gets the command to create a new Mod.
    /// </summary>
    public ICommand CreateModCommand => _createModCommand;

    /// <summary>
    ///     Gets the command to create a new Mixin.
    /// </summary>
    public ICommand CreateMixinCommand => _createMixinCommand;

    /// <summary>
    ///     Gets the command to add an existing project to the Hub.
    /// </summary>
    public ICommand AddExistingCommand => _addExistingCommand;

    /// <summary>
    ///     Gets the action category (null for uncategorized).
    /// </summary>
    public override string? Category => null;

    /// <summary>
    ///     Gets whether this action is globally available.
    /// </summary>
    public override bool IsGlobal => true;

    void OnProjectServiceStateChanged(object? sender, EventArgs e)
    {
        UpdateCanMakeProperties();
        _createScriptCommand.NotifyCanExecuteChanged();
        _createModCommand.NotifyCanExecuteChanged();
        RaiseShouldShowChanged();
    }

    void UpdateCanMakeProperties()
    {
        CanMakeScript = _projectService.State.CanMakeScript;
        CanMakeMod = _projectService.State.CanMakeMod;
    }

    /// <summary>
    ///     Determines whether this action should be visible.
    /// </summary>
    /// <returns>True if at least one project type can be created.</returns>
    public override bool ShouldShow() =>
        // Show if we can create any project type
        CanMakeScript || CanMakeMod;

    async Task CreateScriptAsync() =>
        await CreateProjectAsync("mdk2pbscript",
            "New Programmable Block Script",
            "Create a new Programmable Block Script project",
            "MdkScriptProject",
            new ProgrammableBlockSymbol());

    async Task CreateModAsync() =>
        await CreateProjectAsync("mdk2mod",
            "New Mod",
            "Create a new Space Engineers mod project",
            "MdkModProject",
            new ModSymbol());

    async Task CreateMixinAsync() =>
        await CreateProjectAsync("mdk2mixin",
            "New Mixin",
            "Create a new Space Engineers mixin project",
            "MdkMixinProject",
            new MixinSymbol());

    async Task CreateProjectAsync(string templateName, string title, string description, string defaultProjectName, object icon)
    {
        var defaultLocation = _fileStorage.GetDocumentsPath();

        // Get the last used location for this template
        var hubSettings = _projectService.Settings.GetValue(SettingsKeys.HubSettings, new HubSettings());
        var lastLocation = hubSettings.LastProjectLocationByTemplate.TryGetValue(templateName, out var savedLocation)
            ? savedLocation
            : defaultLocation;

        var dialogViewModel = new NewProjectDialogViewModel(new NewProjectDialogMessage
        {
            Title = title,
            Message = description,
            Icon = icon,
            DefaultProjectName = defaultProjectName,
            DefaultLocation = lastLocation,
            OkText = "Create",
            CancelText = "Cancel"
        });

        await _shell.ShowOverlayAsync(dialogViewModel);
        var result = dialogViewModel.Result;

        if (result == null)
            return; // User cancelled

        // Save the location for next time
        hubSettings.LastProjectLocationByTemplate[templateName] = result.Value.Location;
        _projectService.Settings.SetValue(SettingsKeys.HubSettings, hubSettings);

        // Show busy indicator
        var busyOverlay = new BusyOverlayViewModel("Creating project...");
        var busyTask = _shell.ShowBusyOverlayAsync(busyOverlay);

        try
        {
            // Create the project
            var createResult = await _projectService.CreateProjectAsync(result.Value.ProjectName, result.Value.Location, templateName);

            var projectPath = createResult.ProjectPath;
            var errorMessage = createResult.ErrorMessage;

            if (projectPath == null)
            {
                // Dismiss busy indicator
                busyOverlay.Dismiss();
                await busyTask;

                // Show error
                await _shell.ShowOverlayAsync(new InformationMessage
                {
                    Title = "Project Creation Failed",
                    Message = errorMessage ?? "Unknown error occurred"
                });
                return;
            }

            // Add project to registry
            string? addError;
            if (!_projectService.TryAddProject(projectPath.Value, out addError))
            {
                // Dismiss busy indicator
                busyOverlay.Dismiss();
                await busyTask;

                await _shell.ShowOverlayAsync(new InformationMessage
                {
                    Title = "Failed to Add Project",
                    Message = addError ?? "Unknown error occurred"
                });
                return;
            }

            // Dismiss busy indicator
            busyOverlay.Dismiss();
            await busyTask;

            // Navigate to the new project and open options drawer
            _projectService.NavigateToProject(projectPath.Value, openOptions: true);
        }
        catch (Exception ex)
        {
            // Dismiss busy indicator on exception
            busyOverlay.Dismiss();
            await busyTask;

            await _shell.ShowOverlayAsync(new InformationMessage
            {
                Title = "Unexpected Error",
                Message = $"An error occurred while creating the project: {ex.Message}"
            });
        }
    }

    async Task AddExistingProjectAsync()
    {
        // Get the main window from the application
        var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?
            .Windows.FirstOrDefault(w => w is ShellWindow);

        if (window == null)
            return;

        var filePickerOptions = new FilePickerOpenOptions
        {
            Title = "Select MDK² Project",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("C# Project Files")
                {
                    Patterns = new[] { "*.csproj" }
                }
            }
        };

        var files = await window.StorageProvider.OpenFilePickerAsync(filePickerOptions);
        if (files.Count == 0)
            return;

        var projectPath = files[0].Path.LocalPath;

        if (_projectService.TryAddProject(new CanonicalPath(projectPath), out var errorMessage))
        {
            // Success - the project overview will automatically refresh
        }
        else
        {
            await _shell.ShowOverlayAsync(new ConfirmationMessage
            {
                Title = "Invalid Project",
                Message = errorMessage ?? "The selected file is not a valid MDK² project.",
                OkText = "OK",
                CancelText = "Cancel"
            });
        }
    }
}
