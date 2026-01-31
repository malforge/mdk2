using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Mal.DependencyInjection;
using Mdk.Hub.Features.CommonDialogs;
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
[ViewModelFor<ShellWindow>]
public class ShellViewModel : ViewModel
{
    readonly Dictionary<CanonicalPath, ProjectModel> _projectModels = new();
    readonly ProjectActionsViewModel? _projectActionsViewModel;
    readonly ProjectOverviewViewModel? _projectOverviewViewModel;
    readonly IProjectService? _projectService;
    readonly IShell _shell;
    ViewModel? _currentView;
    ViewModel? _navigationView;

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
    /// <param name="shell">Shell service for access to toast messages.</param>
    /// <param name="settings">Application settings service.</param>
    /// <param name="shell">Common dialogs service.</param>
    /// <param name="projectService">Project service for project management.</param>
    /// <param name="projectOverviewViewModel">Initial content view model displayed in the shell.</param>
    /// <param name="projectActionsViewModel">navigation view model displayed alongside the content.</param>
    /// <param name="updateCheckService">Update check service for monitoring MDK versions.</param>
    /// <param name="announcementService">Announcement service for user notifications.</param>
    public ShellViewModel(IShell shell, ISettings settings, IProjectService projectService, ProjectOverviewViewModel projectOverviewViewModel, ProjectActionsViewModel projectActionsViewModel, IUpdateCheckService updateCheckService, Announcements.IAnnouncementService announcementService)
    {
        _shell = shell;
        _shell = shell;
        _projectService = projectService;
        _projectOverviewViewModel = projectOverviewViewModel;
        _projectActionsViewModel = projectActionsViewModel;
        OverlayViews.CollectionChanged += OnOverlayViewsCollectionChanged;
        Settings = settings;
        NavigationView = projectOverviewViewModel;
        CurrentView = projectActionsViewModel;

        // Initialize child VMs with reference to this shell
        projectOverviewViewModel.Initialize(this);
        projectActionsViewModel.Initialize(this);

        // Listen for navigation requests to coordinate OpenOptions flag
        projectService.ProjectNavigationRequested += OnProjectNavigationRequested;

        // Check for first-run setup (fire and forget)
        if (Program.IsFirstRun)
            _ = CheckFirstRunSetupAsync(updateCheckService, shell);

        // Start background checks on startup (fire and forget)
        _ = updateCheckService.CheckForUpdatesAsync();
        _ = announcementService.CheckForAnnouncementsAsync();
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
    ///     Gets the collection of overlay view models that represents additional layers
    ///     of content or UI elements displayed over the main content.
    /// </summary>
    /// <remarks>
    ///     Overlay views are typically used to represent transient or context-sensitive
    ///     UI elements, such as dialogs, notifications, or pop-ups, that must appear
    ///     on top of the main application content.
    /// </remarks>
    public ObservableCollection<ViewModel> OverlayViews { get; } = new();

    /// <summary>
    ///     Gets the collection of toast messages displayed non-blockingly.
    /// </summary>
    public ObservableCollection<ToastMessage> ToastMessages => IsDesignMode ? new ObservableCollection<ToastMessage>() : _shell.ToastMessages;

    /// <summary>
    ///     Raised when the ViewModel requests the window to close.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    ///     Notifies the ViewModel that the window focus was gained.
    /// </summary>
    public void WindowFocusWasGained()
    {
        if (_shell is Shell shellService)
            shellService.RaiseWindowFocusGained();
    }

    /// <summary>
    ///     Checks if the window can close. Returns false if there are unsaved changes and user cancels.
    /// </summary>
    public async Task<bool> CanCloseAsync()
    {
        if (!_shell.TryGetUnsavedChangesInfo(out var info))
            return true;

        if (_shell == null)
            return true;

        var result = await _shell.ShowAsync(new ConfirmationMessage
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
    ///     Requests the window to close programmatically (bypasses CanClose check).
    /// </summary>
    public void RequestClose() => CloseRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    ///     Requests a refresh of all UI components.
    /// </summary>
    public void RequestRefresh() => _shell?.RequestRefresh();

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
        if (_shell == null || _projectService == null)
            throw new InvalidOperationException("Cannot create ProjectModel in design mode");

        var model = new ProjectModel(
            projectInfo.Type,
            projectInfo.Name,
            projectInfo.ProjectPath,
            projectInfo.LastReferenced,
            _shell,
            _projectService);

        model.UpdateFromProjectInfo(projectInfo);
        _projectModels[projectInfo.ProjectPath] = model;

        return model;
    }

    async Task CheckFirstRunSetupAsync(IUpdateCheckService updateCheckService, IShell commonShell)
    {
        try
        {
            // Show busy overlay while checking/installing prerequisites
            var busyOverlay = new BusyOverlayViewModel("Setting up MDK² for first use...");
            _shell.AddOverlay(busyOverlay);

            try
            {
                var messages = new List<string>();

                // Check and install .NET SDK
                busyOverlay.Message = "Checking .NET SDK...";
                var (sdkInstalled, sdkVersion) = await updateCheckService.CheckDotNetSdkAsync();
                if (!sdkInstalled)
                {
                    busyOverlay.Message = "Installing .NET SDK... (this may take a few minutes)";
                    try
                    {
                        await updateCheckService.InstallDotNetSdkAsync();
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
                var templateInstalled = await updateCheckService.IsTemplateInstalledAsync();
                if (!templateInstalled)
                {
                    busyOverlay.Message = "Installing MDK² template package...";
                    try
                    {
                        await updateCheckService.InstallTemplateAsync();
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

                await _shell.ShowAsync(new InformationMessage
                {
                    Title = "First-Run Setup Complete",
                    Message = string.Join("\n", messages)
                });
            }
            catch (Exception ex)
            {
                busyOverlay.Dismiss();
                await _shell.ShowAsync(new InformationMessage
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

    void OnProjectNavigationRequested(object? sender, ProjectNavigationRequestedEventArgs e)
    {
        // ProjectService has already updated State.SelectedProject
        // This event is only for post-selection actions like opening options drawer
        if (e.OpenOptions)
            _projectActionsViewModel?.OpenOptionsDrawer();
    }
}