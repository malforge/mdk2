using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Projects.Actions;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Settings;
using Mdk.Hub.Framework;

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
[Dependency]
[ViewModelFor<ShellWindow>]
public class ShellViewModel : ViewModel
{
    readonly IShell _shell;
    readonly Mdk.Hub.Features.CommonDialogs.ICommonDialogs? _commonDialogs;
    ViewModel? _currentView;
    ViewModel? _navigationView;

    /// <summary>
    ///     Raised when the ViewModel requests the window to close.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    ///     Parameterless constructor intended for design-time tooling. Initializes the instance in design mode.
    /// </summary>
    public ShellViewModel() : this(null!, null!, null!, null!, null!)
    {
        IsDesignMode = true;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ShellViewModel" /> for runtime usage.
    /// </summary>
    /// <param name="shell">Shell service for access to toast messages.</param>
    /// <param name="settings">Application settings service.</param>
    /// <param name="commonDialogs">Common dialogs service.</param>
    /// <param name="projectOverviewViewModel">Initial content view model displayed in the shell.</param>
    /// <param name="projectActionsViewModel">navigation view model displayed alongside the content.</param>
    public ShellViewModel(IShell shell, ISettings settings, Mdk.Hub.Features.CommonDialogs.ICommonDialogs commonDialogs, ProjectOverviewViewModel projectOverviewViewModel, ProjectActionsViewModel projectActionsViewModel)
    {
        _shell = shell;
        _commonDialogs = commonDialogs;
        OverlayViews.CollectionChanged += OnOverlayViewsCollectionChanged;
        Settings = settings;
        NavigationView = projectOverviewViewModel;
        CurrentView = projectActionsViewModel;
    }
    
    /// <summary>
    ///     Initializes easter egg. Should be called from view once easter egg control is available.
    /// </summary>
    public void InitializeEasterEgg(EasterEgg easterEggControl)
    {
        easterEggControl?.Initialize(_shell);
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
    public ObservableCollection<ToastMessage> ToastMessages => IsDesignMode ? new() : _shell.ToastMessages;

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
    public async System.Threading.Tasks.Task<bool> CanCloseAsync()
    {
        if (!_shell.TryGetUnsavedChangesInfo(out var info))
            return true;

        if (_commonDialogs == null)
            return true;

        var result = await _commonDialogs.ShowAsync(new Mdk.Hub.Features.CommonDialogs.ConfirmationMessage
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
        info.GoThereAction?.Invoke();
        return false;
    }

    /// <summary>
    ///     Requests the window to close programmatically (bypasses CanClose check).
    /// </summary>
    public void RequestClose()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    void OnOverlayViewsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(HasOverlays));
}