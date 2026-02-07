using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.Announcements;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Projects;
using Mdk.Hub.Features.Projects.Actions;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Settings;
using Mdk.Hub.Features.Updates;
using Mdk.Hub.Framework;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Shell;

/// <summary>
///     View model for the application shell window. It orchestrates top-level UI composition by
///     exposing the current content view, an optional navigation view, and an optional overlay view.
/// </summary>
/// <remarks>
///     This view model is registered for dependency injection and associated with <c>ShellWindow</c>
///     via attributes. However for the most part you should rely on the <c>IShell</c> service to
///     manage the shell and its content rather than interacting with this view model directly.
/// </remarks>
[Singleton]
[Singleton<IShell>]
[ViewModelFor<ShellWindow>]
public class ShellViewModel : ViewModel, IShell
{
    readonly Lazy<IAnnouncementService> _lazyAnnouncementService;
    readonly Lazy<IProjectService> _lazyProjectService;
    readonly Lazy<IUpdateManager> _lazyUpdateManager;
    readonly ILogger _logger;
    readonly Lazy<ProjectActionsViewModel> _projectActionsViewModel;
    readonly Dictionary<CanonicalPath, ProjectModel> _projectModels = new();
    readonly Lazy<ProjectOverviewViewModel> _projectOverviewViewModel;
    readonly List<Action<string[]>> _readyCallbacks = new();
    readonly List<Action<string[]>> _startupCallbacks = new();
    readonly List<UnsavedChangesRegistration> _unsavedChangesRegistrations = new();
    ViewModel? _currentView;
    bool _hasStarted;
    WindowState? _initialWindowState;
    bool _isInBackground;
    bool _isReady;
    ViewModel? _navigationView;
    string[]? _startupArgs;

    /// <summary>
    ///     Parameterless constructor intended for design-time tooling. Initializes the instance in design mode.
    /// </summary>
    public ShellViewModel() : this(null!, null!, null!, null!, null!, null!, null!)
    {
        IsDesignMode = true;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ShellViewModel" /> for runtime usage.
    /// </summary>
    /// <param name="settings">Application settings service.</param>
    /// <param name="lazyProjectService">Project service for project management.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="lazyAnnouncementService">Announcement service for user notifications.</param>
    /// <param name="projectOverviewViewModel">Initial content view model displayed in the shell.</param>
    /// <param name="projectActionsViewModel">navigation view model displayed alongside the content.</param>
    /// <param name="lazyUpdateManager">Update manager service for monitoring MDK versions.</param>
    public ShellViewModel(
        ISettings settings,
        Lazy<IProjectService> lazyProjectService,
        Lazy<IUpdateManager> lazyUpdateManager,
        Lazy<IAnnouncementService> lazyAnnouncementService,
        ILogger logger,
        Lazy<ProjectOverviewViewModel> projectOverviewViewModel,
        Lazy<ProjectActionsViewModel> projectActionsViewModel)
    {
        _lazyProjectService = lazyProjectService;
        _lazyUpdateManager = lazyUpdateManager;
        _lazyAnnouncementService = lazyAnnouncementService;
        _logger = logger;
        _projectOverviewViewModel = projectOverviewViewModel;
        _projectActionsViewModel = projectActionsViewModel;
        Settings = settings;
        // NavigationView = projectOverviewViewModel;
        // CurrentView = projectActionsViewModel;

        // Subscribe to overlay collection changes for HasOverlays property
        if (!IsDesignMode)
            OverlayViews.CollectionChanged += OnOverlayViewsCollectionChanged;
    }

    /// <summary>
    ///     Gets a value indicating whether the instance has been created for design-time usage.
    /// </summary>
    public bool IsDesignMode { get; }

    /// <summary>
    ///     Gets the application settings service accessible to shell-level views.
    /// </summary>
    public ISettings Settings { get; }

    /// <summary>
    ///     Gets the view model currently displayed in the main content area of the shell.
    /// </summary>
    /// <remarks>
    ///     The setter is internal to the shell; navigation should generally be performed via shell logic/services.
    /// </remarks>
    public ViewModel? CurrentView
    {
        get => _currentView;
        private set => SetProperty(ref _currentView, value);
    }

    /// <summary>
    ///     Gets the optional navigation pane view model (e.g., sidebar or menu) shown alongside the content.
    /// </summary>
    /// <remarks>
    ///     Managed internally by the shell to show or hide contextual navigation.
    /// </remarks>
    public ViewModel? NavigationView
    {
        get => _navigationView;
        private set => SetProperty(ref _navigationView, value);
    }

    /// <summary>
    ///     Gets a value indicating whether there are currently any overlays present in the shell's overlay views.
    /// </summary>
    public bool HasOverlays => OverlayViews.Count > 0;

    /// <summary>
    ///     Raised when the window gains keyboard focus.
    /// </summary>
    public event EventHandler? WindowFocusGained;
    
    /// <summary>
    ///     Raised when a UI refresh has been requested (e.g., Ctrl+R).
    /// </summary>
    public event EventHandler? RefreshRequested;

    /// <summary>
    ///     Gets the collection of currently displayed toast messages.
    /// </summary>
    public ObservableCollection<ToastMessage> ToastMessages { get; } = new();
    
    /// <summary>
    ///     Gets the collection of overlay views (dialogs, popups) currently shown over the main content.
    /// </summary>
    public ObservableCollection<OverlayModel> OverlayViews { get; } = new();

    /// <summary>
    ///     Gets the initial window state to use when launching with notification arguments.
    ///     Null means use the saved window settings.
    /// </summary>
    public WindowState? InitialWindowState => _initialWindowState;

    /// <inheritdoc />
    public bool IsInBackground
    {
        get => _isInBackground;
        private set
        {
            if (_isInBackground != value)
            {
                _isInBackground = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    ///     Starts the shell with the specified command-line arguments and initializes the main UI.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    public void Start(string[] args)
    {
        _startupArgs = args;
        _hasStarted = true;
        
        // Start minimized if launched with notification arguments
        // HandleBuildNotificationAsync will call BringToFront() if InteractiveMode is OpenHub
        if (IsNotificationCommand(args))
            _initialWindowState = WindowState.Minimized;
        
        NavigationView = _projectOverviewViewModel.Value;
        CurrentView = _projectActionsViewModel.Value;

        // Write Hub executable path for MDK CLI to discover
        WriteHubPath();

        BeginStartup();
    }
    
    static bool IsNotificationCommand(string[] args)
    {
        if (args.Length < 3)
            return false;
        
        var command = args[0].ToLowerInvariant();
        return command is "script" or "mod" or "custom" or "nuget";
    }

    /// <summary>
    ///     Registers a callback to be invoked when the shell has started.
    /// </summary>
    /// <param name="callback">Action to invoke with startup arguments.</param>
    public void WhenStarted(Action<string[]> callback)
    {
        if (_hasStarted)
            callback(_startupArgs!);
        else
            _startupCallbacks.Add(callback);
    }

    /// <summary>
    ///     Registers a callback to be invoked when the shell is fully ready (all async initialization complete).
    /// </summary>
    /// <param name="callback">Action to invoke with startup arguments.</param>
    public void WhenReady(Action<string[]> callback)
    {
        if (_isReady)
            callback(_startupArgs!);
        else
            _readyCallbacks.Add(callback);
    }

    /// <inheritdoc />
    public void AddOverlay(OverlayModel model)
    {
        void onDismissed(object? sender, EventArgs e)
        {
            model.Dismissed -= onDismissed;
            OverlayViews.Remove(model);
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (model is IDisposable disposable) disposable.Dispose();
        }

        model.Dismissed += onDismissed;
        OverlayViews.Add(model);
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        _logger.Info("Shutdown requested");
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void BringToFront()
    {
        _logger.Info("Bringing window to front");
        BringToFrontRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void ShowToast(string message, int durationMs = 3000)
    {
        var toast = new ToastMessage { Message = message };
        ToastMessages.Add(toast);

        // Start dismiss animation before removal
        Task.Delay(durationMs - 200).ContinueWith(_ => { toast.IsDismissing = true; }, TaskScheduler.FromCurrentSynchronizationContext());

        // Remove after fade-out animation completes
        Task.Delay(durationMs).ContinueWith(_ => { ToastMessages.Remove(toast); }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    /// <inheritdoc />
    public UnsavedChangesHandle RegisterUnsavedChanges(string description, Action navigateToChanges)
    {
        var registration = new UnsavedChangesRegistration
        {
            Description = description,
            NavigateAction = navigateToChanges
        };

        _unsavedChangesRegistrations.Add(registration);

        return new UnsavedChangesHandle(() => _unsavedChangesRegistrations.Remove(registration));
    }

    /// <inheritdoc />
    public bool TryGetUnsavedChangesInfo(out UnsavedChangesInfo info)
    {
        if (_unsavedChangesRegistrations.Count == 0)
        {
            info = default;
            return false;
        }

        if (_unsavedChangesRegistrations.Count == 1)
        {
            // Single registration: use its description and action
            var registration = _unsavedChangesRegistrations[0];
            info = new UnsavedChangesInfo
            {
                Description = registration.Description,
                GoThereAction = registration.NavigateAction
            };
        }
        else
        {
            // Multiple registrations: generic message with no-op action
            info = new UnsavedChangesInfo
            {
                Description = "You have unsaved changes.",
                GoThereAction = () => { }
            };
        }

        return true;
    }

    /// <inheritdoc />
    public void RequestRefresh()
    {
        RefreshRequested?.Invoke(this, EventArgs.Empty);
        ShowToast("Refreshing...", 1500);
    }

    /// <inheritdoc />
    public Task ShowOverlayAsync(OverlayModel model)
    {
        var tcs = new TaskCompletionSource();

        void handler(object? sender, EventArgs e)
        {
            model.Dismissed -= handler;
            tcs.SetResult();
        }

        model.Dismissed += handler;
        AddOverlay(model);
        return tcs.Task;
    }

    /// <inheritdoc />
    public async Task<bool> ShowOverlayAsync(ConfirmationMessage message)
    {
        var model = new MessageBoxViewModel
        {
            Title = message.Title,
            Message = message.Message,
            Choices =
            [
                new MessageBoxChoice
                {
                    Text = message.OkText,
                    Value = true
                },
                new MessageBoxChoice
                {
                    Text = message.CancelText,
                    Value = false
                }
            ]
        };

        await ShowOverlayAsync(model);
        return (bool)(model.SelectedValue ?? false);
    }

    /// <inheritdoc />
    public async Task ShowOverlayAsync(InformationMessage message)
    {
        var model = new MessageBoxViewModel
        {
            Title = message.Title,
            Message = message.Message,
            Choices =
            [
                new MessageBoxChoice
                {
                    Text = message.OkText,
                    Value = true,
                    IsDefault = true
                }
            ]
        };

        await ShowOverlayAsync(model);
    }

    /// <inheritdoc />
    public async Task<bool> ShowOverlayAsync(KeyPhraseValidationMessage message)
    {
        var model = new DangerBoxViewModel
        {
            Title = message.Title,
            Message = message.Message,
            RequiredKeyPhrase = message.RequiredKeyPhrase,
            KeyPhraseWatermark = message.KeyPhraseWatermark,
            Choices =
            [
                new DangerousMessageBoxChoice
                {
                    Text = message.OkText,
                    Value = true,
                    IsDefault = true
                },
                new MessageBoxChoice
                {
                    Text = message.CancelText,
                    Value = false,
                    IsCancel = true
                }
            ]
        };

        await ShowOverlayAsync(model);
        return (bool)(model.SelectedValue ?? false);
    }

    /// <inheritdoc />
    public async Task ShowBusyOverlayAsync(BusyOverlayViewModel busyOverlay) => await ShowOverlayAsync(busyOverlay);

    /// <summary>
    ///     Begins asynchronous startup process without blocking initialization.
    /// </summary>
    async void BeginStartup()
    {
        try
        {
            await StartupAsync();
        }
        catch (Exception e)
        {
            _logger.Error("Error during startup", e);
            Shutdown();
        }
    }

    /// <summary>
    ///     Performs asynchronous startup initialization including first-run setup and Linux path configuration.
    /// </summary>
    async Task StartupAsync()
    {
        // Invoke queued callbacks
        foreach (var callback in _startupCallbacks)
            callback(_startupArgs ?? Array.Empty<string>());
        _startupCallbacks.Clear();

        // Check for first-run setup (fire and forget)
        if (Program.IsFirstRun)
            await CheckFirstRunSetupAsync(_lazyUpdateManager.Value);

        if (!await CheckLinuxPathConfigurationAsync())
        {
            // User cancelled Linux path configuration - exit application
            Shutdown();
            return;
        }

        // Mark shell as ready
        OnReady();
    }

    /// <summary>
    ///     Raised when the ViewModel requests the window to close.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    ///     Raised when the ViewModel requests the window to be brought to the front.
    /// </summary>
    public event EventHandler? BringToFrontRequested;

    /// <summary>
    ///     Notifies the ViewModel that the window focus was gained.
    /// </summary>
    public void WindowFocusWasGained() => RaiseWindowFocusGained();

    /// <summary>
    ///     Updates whether the window is in the background (minimized or not focused).
    /// </summary>
    /// <param name="isMinimized">True if window is minimized.</param>
    /// <param name="isFocused">True if window has focus.</param>
    public void UpdateWindowState(bool isMinimized, bool isFocused)
    {
        IsInBackground = isMinimized || !isFocused;
    }

    /// <summary>
    ///     Checks if the window can close. Returns false if there are unsaved changes and user cancels.
    /// </summary>
    public async Task<bool> CanCloseAsync()
    {
        if (!TryGetUnsavedChangesInfo(out var info))
            return true;

        var result = await ShowOverlayAsync(new ConfirmationMessage
        {
            Title = "Unsaved Changes",
            Message = info.Description,
            OkText = "Exit Anyway",
            CancelText = "Show Me"
        });

        if (result)
        {
            // User chose "Exit Anyway" - allow close
            return true;
        }

        // User chose "Show Me" - execute navigation action and don't close
        info.GoThereAction.Invoke();
        return false;
    }

    /// <summary>
    ///     Handles changes to the overlay views collection to update HasOverlays property.
    /// </summary>
    void OnOverlayViewsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(HasOverlays));

    /// <summary>
    ///     Gets an existing ProjectModel or creates a new one for the specified project.
    ///     This ensures both ProjectOverviewViewModel and ProjectActionsViewModel share the same instances.
    /// </summary>
    public ProjectModel GetOrCreateProjectModel(ProjectInfo projectInfo)
    {
        if (_projectModels.TryGetValue(projectInfo.ProjectPath, out var existing))
        {
            // Update existing model with latest info
            existing.UpdateFromProjectInfo(projectInfo);
            return existing;
        }

        // Create new model
        if (_lazyProjectService == null)
            throw new InvalidOperationException("Cannot create ProjectModel in design mode");

        var model = new ProjectModel(
            projectInfo.Type,
            projectInfo.Name,
            projectInfo.ProjectPath,
            projectInfo.LastReferenced,
            this,
            _lazyProjectService.Value);

        model.UpdateFromProjectInfo(projectInfo);
        _projectModels[projectInfo.ProjectPath] = model;

        return model;
    }

    /// <summary>
    ///     Checks if this is the first run and performs initial setup including .NET SDK and template installation.
    /// </summary>
    /// <param name="updateManager">The update manager service.</param>
    async Task CheckFirstRunSetupAsync(IUpdateManager updateManager)
    {
        try
        {
            // Show busy overlay while checking/installing prerequisites
            var busyOverlay = new BusyOverlayViewModel("Setting up MDK² for first use...");
            AddOverlay(busyOverlay);

            try
            {
                var messages = new List<string>();

                // Check and install .NET SDK
                busyOverlay.Message = "Checking .NET SDK...";
                var (sdkInstalled, sdkVersion) = await updateManager.CheckDotNetSdkAsync();
                if (!sdkInstalled)
                {
                    busyOverlay.Message = "Installing .NET SDK... (this may take a few minutes)";
                    try
                    {
                        await updateManager.InstallDotNetSdkAsync();
                        messages.Add("✓ .NET SDK installed successfully");
                    }
                    catch (Exception ex)
                    {
                        messages.Add($"✗ Failed to install .NET SDK: {ex.Message}");
                    }
                }
                else
                    messages.Add($"✓ .NET SDK {sdkVersion} is already installed");

                // Check and install template package
                busyOverlay.Message = "Checking template package...";
                var templateInstalled = await updateManager.IsTemplateInstalledAsync();
                if (!templateInstalled)
                {
                    busyOverlay.Message = "Installing MDK² template package...";
                    try
                    {
                        await updateManager.InstallTemplateAsync();
                        messages.Add("✓ MDK² template package installed successfully");
                    }
                    catch (Exception ex)
                    {
                        messages.Add($"✗ Failed to install template package: {ex.Message}");
                    }
                }
                else
                    messages.Add("✓ MDK² template package is already installed");

                // Show results
                busyOverlay.Dismiss();

                await ShowOverlayAsync(new InformationMessage
                {
                    Title = "First-Run Setup Complete",
                    Message = string.Join("\n", messages)
                });
            }
            catch (Exception ex)
            {
                busyOverlay.Dismiss();
                await ShowOverlayAsync(new InformationMessage
                {
                    Title = "Setup Error",
                    Message = $"An error occurred during first-run setup:\n\n{ex.Message}"
                });
            }
        }
        catch
        {
            // Silently fail if we can't check for first-run
        }
    }

    /// <summary>
    ///     Handles project navigation requests to coordinate post-selection actions like opening the options drawer.
    /// </summary>
    void OnProjectNavigationRequested(object? sender, ProjectNavigationRequestedEventArgs e)
    {
        // ProjectService has already updated State.SelectedProject
        // This event is only for post-selection actions like opening options drawer
        if (e.OpenOptions)
            _projectActionsViewModel.Value.OpenOptionsDrawer();
    }

    /// <summary>
    ///     Called when the shell is fully ready to initialize child view models and start background services.
    /// </summary>
    void OnReady()
    {
        _isReady = true;
        _logger.Info("Shell is ready for operation");

        // Initialize child VMs when ready
        _projectOverviewViewModel.Value.Initialize(this);
        _projectActionsViewModel.Value.Initialize(this, _projectOverviewViewModel.Value);

        // Listen for navigation requests to coordinate OpenOptions flag
        _lazyProjectService.Value.ProjectNavigationRequested += OnProjectNavigationRequested;
        _lazyUpdateManager.Value.CheckForUpdatesAsync();
        _lazyAnnouncementService.Value.CheckForAnnouncementsAsync();

        foreach (var callback in _readyCallbacks)
            callback(_startupArgs!);
        _readyCallbacks.Clear();

        // Check if we should prompt about prerelease updates
        Task.Delay(1000).ContinueWith(_ => CheckPrereleasePrompt());
    }

    /// <summary>
    ///     Checks if Linux path configuration is required (any path set to "auto").
    /// </summary>
    bool RequiresLinuxPathConfiguration()
    {
        var hubSettings = Settings.GetValue(SettingsKeys.HubSettings, new HubSettings());
        var binPath = hubSettings.CustomAutoBinaryPath;
        var scriptOutputPath = hubSettings.CustomAutoScriptOutputPath;
        var modOutputPath = hubSettings.CustomAutoModOutputPath;

        return binPath == "auto" || scriptOutputPath == "auto" || modOutputPath == "auto";
    }

    /// <summary>
    ///     Opens the global settings dialog for Linux path configuration and waits for completion.
    /// </summary>
    /// <returns>True if configuration is now valid; otherwise, false.</returns>
    async Task<bool> ConfigureGlobalOptionsForLinuxAsync()
    {
        var viewModel = App.Container.Resolve<GlobalSettingsViewModel>();
        TaskCompletionSource tcs = new();
        viewModel.MarkAsOpenedForLinuxValidation();

        // When dismissed, check if we can now set ready
        viewModel.Dismissed += (_, _) => { tcs.SetResult(); };
        AddOverlay(viewModel);
        await tcs.Task;
        // Return whether configuration is now valid
        return !RequiresLinuxPathConfiguration();
    }

    /// <summary>
    ///     Raises the WindowFocusGained event to notify subscribers that the window has gained focus.
    /// </summary>
    public void RaiseWindowFocusGained() => WindowFocusGained?.Invoke(this, EventArgs.Empty);

    /// <summary>
    ///     Writes the Hub executable path to a file for MDK CLI to discover.
    /// </summary>
    void WriteHubPath()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (exePath == null)
            {
                _logger.Warning("Could not determine Hub executable path");
                return;
            }

            // Resolve to actual path if it's a symlink (Velopack 'current' directory)
            var resolvedPath = ResolveSymlink(exePath);

            // Write to %AppData%/MDK2/hub.path or ~/.config/MDK2/hub.path
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var mdkFolder = Path.Combine(appDataFolder, "MDK2");
            Directory.CreateDirectory(mdkFolder);

            var pathFile = Path.Combine(mdkFolder, "hub.path");
            File.WriteAllText(pathFile, resolvedPath);

            _logger.Info($"Hub path written to: {pathFile}");
            _logger.Debug($"Hub executable: {resolvedPath}");
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to write Hub path file: {ex.Message}");
        }
    }

    /// <summary>
    ///     Checks if Linux path configuration is valid and prompts for configuration if needed.
    /// </summary>
    /// <returns>True if configuration is valid or user completed configuration; false if user cancelled.</returns>
    async Task<bool> CheckLinuxPathConfigurationAsync()
    {
        if (!App.IsLinux)
            return true;

        var hubSettings = Settings.GetValue(SettingsKeys.HubSettings, new HubSettings());
        var binPath = hubSettings.CustomAutoBinaryPath;
        var scriptOutputPath = hubSettings.CustomAutoScriptOutputPath;
        var modOutputPath = hubSettings.CustomAutoModOutputPath;

        // On Linux, "auto" is not acceptable - paths must be explicitly set
        if (binPath == "auto" || scriptOutputPath == "auto" || modOutputPath == "auto")
        {
            _logger.Info($"Linux: Required paths not configured (bin={binPath}, script={scriptOutputPath}, mod={modOutputPath}), opening global options");

            return await ConfigureGlobalOptionsForLinuxAsync();
        }

        // Paths are valid
        return true;
    }

    /// <summary>
    ///     Resolves a symbolic link to its target path.
    /// </summary>
    /// <param name="path">The path to resolve.</param>
    /// <returns>The target path if it's a valid symlink; otherwise, the original path.</returns>
    static string ResolveSymlink(string path)
    {
        try
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.LinkTarget != null)
            {
                var targetPath = Path.GetFullPath(fileInfo.LinkTarget, Path.GetDirectoryName(path) ?? string.Empty);
                return File.Exists(targetPath) ? targetPath : path;
            }

            return path;
        }
        catch
        {
            return path;
        }
    }

    /// <summary>
    ///     Checks if the user should be prompted to enable prerelease updates when running a prerelease version.
    /// </summary>
    async void CheckPrereleasePrompt()
    {
        try
        {
            // Get current version
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            // Strip git metadata
            if (version != null)
            {
                var plusIndex = version.IndexOf('+');
                if (plusIndex >= 0)
                    version = version.Substring(0, plusIndex);
            }

            if (string.IsNullOrEmpty(version))
                return;

            // Check if current version is a prerelease (contains -)
            var isPrerelease = version.Contains('-');
            if (!isPrerelease)
                return;

            var hubSettings = Settings.GetValue(SettingsKeys.HubSettings, new HubSettings());

            // If already prompted or prereleases already enabled, skip
            if (hubSettings.HasPromptedForPrereleaseUpdates || hubSettings.IncludePrereleaseUpdates)
                return;

            _logger.Info($"Running prerelease version {version} with prerelease updates disabled - prompting user");

            // Prompt user
            var result = await ShowOverlayAsync(new ConfirmationMessage
            {
                Title = "Enable Prerelease Updates?",
                Message = $"You're running a prerelease version of MDK Hub ({version}).\n\nWould you like to enable prerelease updates? This will allow the Hub to check for and install newer prerelease versions automatically.\n\nYou can change this setting later in the Settings screen.",
                OkText = "Enable Prerelease Updates",
                CancelText = "No Thanks"
            });

            // Save the choice and mark as prompted
            hubSettings.IncludePrereleaseUpdates = result;
            hubSettings.HasPromptedForPrereleaseUpdates = true;
            Settings.SetValue(SettingsKeys.HubSettings, hubSettings);

            _logger.Info($"User {(result ? "enabled" : "declined")} prerelease updates");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error checking prerelease prompt: {ex.Message}");
        }
    }

    /// <summary>
    ///     Represents a registration for tracking unsaved changes with a description and navigation action.
    /// </summary>
    class UnsavedChangesRegistration
    {
        /// <summary>
        ///     Gets or initializes the description of the unsaved changes.
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        ///     Gets or initializes the action to navigate to the location of the unsaved changes.
        /// </summary>
        public Action NavigateAction { get; init; } = () => { };
    }
}
