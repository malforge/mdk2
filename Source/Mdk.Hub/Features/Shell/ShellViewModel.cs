using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Mal.DependencyInjection;
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
    ViewModel? _currentView;
    ViewModel? _navigationView;

    /// <summary>
    ///     Parameterless constructor intended for design-time tooling. Initializes the instance in design mode.
    /// </summary>
    public ShellViewModel() : this(null!, null!)
    {
        IsDesignMode = true;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ShellViewModel" /> for runtime usage.
    /// </summary>
    /// <param name="settings">Application settings service.</param>
    /// <param name="projectOverviewViewModel">Initial content view model displayed in the shell.</param>
    public ShellViewModel(ISettings settings, ProjectOverviewViewModel projectOverviewViewModel)
    {
        OverlayViews.CollectionChanged += OnOverlayViewsCollectionChanged;
        Settings = settings;
        CurrentView = projectOverviewViewModel;
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

    void OnOverlayViewsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(HasOverlays));
}