using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Projects.NewProjectDialog;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Shell;

public interface IShell
{
    /// <summary>
    ///     Gets the collection of active toast messages.
    /// </summary>
    ObservableCollection<ToastMessage> ToastMessages { get; }
    
    /// <summary>
    ///     Gets the collection of overlay view models (dialogs).
    /// </summary>
    ObservableCollection<OverlayModel> OverlayViews { get; }

    void Start(string[] args);

    /// <summary>
    ///     Registers a callback to be invoked when Shell has started.
    ///     If already started, invokes immediately. Otherwise queues until Start() is called.
    /// </summary>
    void WhenStarted(Action<string[]> callback);
    
    /// <summary>
    ///     Registers a callback to be invoked when Shell is ready for operation.
    ///     On Linux, this means required paths are configured. Otherwise, ready immediately after start.
    ///     If already ready, invokes immediately. Otherwise queues until ready.
    /// </summary>
    void WhenReady(Action<string[]> callback);

    void AddOverlay(OverlayModel model);

    /// <summary>
    ///     Shows a non-blocking toast message that auto-dismisses.
    /// </summary>
    void ShowToast(string message, int durationMs = 3000);

    /// <summary>
    ///     Raised when the main window gains focus.
    /// </summary>
    event EventHandler? WindowFocusGained;

    /// <summary>
    ///     Raised when a refresh is explicitly requested (e.g., Ctrl+R).
    /// </summary>
    event EventHandler? RefreshRequested;

    /// <summary>
    ///     Requests a refresh of all UI components that support it.
    /// </summary>
    void RequestRefresh();

    /// <summary>
    ///     Registers unsaved changes with a description and navigation action.
    ///     Returns a handle that must be disposed when changes are saved.
    /// </summary>
    UnsavedChangesHandle RegisterUnsavedChanges(string description, Action navigateToChanges);

    /// <summary>
    ///     Tries to get unsaved changes information for display in dialogs.
    /// </summary>
    /// <param name="info">Info containing description and navigation action. For multiple registrations, action is empty.</param>
    /// <returns>True if there are unsaved changes, false otherwise.</returns>
    bool TryGetUnsavedChangesInfo(out UnsavedChangesInfo info);

    /// <summary>
    ///     Requests the application to shut down gracefully.
    /// </summary>
    void Shutdown();

    // Dialog methods
    Task ShowOverlayAsync(OverlayModel model);
    Task<bool> ShowOverlayAsync(ConfirmationMessage message);
    Task ShowOverlayAsync(InformationMessage message);
    Task<bool> ShowOverlayAsync(KeyPhraseValidationMessage message);
    Task ShowBusyOverlayAsync(BusyOverlayViewModel busyOverlay);
}

public class ToastMessage : ViewModel
{
    bool _isDismissing;
    public string Message { get; set; } = string.Empty;

    public bool IsDismissing
    {
        get => _isDismissing;
        set => SetProperty(ref _isDismissing, value);
    }
}