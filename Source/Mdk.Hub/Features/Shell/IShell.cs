using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Projects.NewProjectDialog;

namespace Mdk.Hub.Features.Shell;

/// <summary>
///     Core shell interface providing UI services, overlay management, and lifecycle control.
/// </summary>
public interface IShell
{
    /// <summary>
    ///     Gets the collection of active toast notification messages.
    /// </summary>
    ObservableCollection<ToastMessage> ToastMessages { get; }
    
    /// <summary>
    ///     Gets the collection of overlay view models currently displayed as dialogs.
    /// </summary>
    ObservableCollection<OverlayModel> OverlayViews { get; }

    /// <summary>
    ///     Starts the shell with the provided command-line arguments.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    void Start(string[] args);

    /// <summary>
    ///     Registers a callback to be invoked when the shell has started.
    ///     If already started, invokes immediately. Otherwise queues until Start() is called.
    /// </summary>
    /// <param name="callback">Callback receiving the startup arguments.</param>
    void WhenStarted(Action<string[]> callback);
    
    /// <summary>
    ///     Registers a callback to be invoked when the shell is ready for operation.
    ///     On Linux, this means required paths are configured. Otherwise, ready immediately after start.
    ///     If already ready, invokes immediately. Otherwise queues until ready.
    /// </summary>
    /// <param name="callback">Callback receiving the startup arguments.</param>
    void WhenReady(Action<string[]> callback);

    /// <summary>
    ///     Adds an overlay view model to the shell's overlay collection for display.
    /// </summary>
    /// <param name="model">The overlay view model to display.</param>
    void AddOverlay(OverlayModel model);

    /// <summary>
    ///     Shows a non-blocking toast notification message that auto-dismisses after a duration.
    /// </summary>
    /// <param name="message">The message text to display.</param>
    /// <param name="durationMs">Duration in milliseconds before auto-dismiss (default 3000ms).</param>
    void ShowToast(string message, int durationMs = 3000);

    /// <summary>
    ///     Raised when the main window gains focus.
    /// </summary>
    event EventHandler? WindowFocusGained;

    /// <summary>
    ///     Raised when a UI refresh is explicitly requested (e.g., via Ctrl+R).
    /// </summary>
    event EventHandler? RefreshRequested;

    /// <summary>
    ///     Requests a refresh of all UI components that support refreshing.
    /// </summary>
    void RequestRefresh();

    /// <summary>
    ///     Registers unsaved changes with a description and navigation action.
    ///     Returns a handle that must be disposed when changes are saved.
    /// </summary>
    /// <param name="description">Description of what changes are unsaved.</param>
    /// <param name="navigateToChanges">Action to navigate the user to the unsaved changes.</param>
    /// <returns>A handle to dispose when changes are saved.</returns>
    UnsavedChangesHandle RegisterUnsavedChanges(string description, Action navigateToChanges);

    /// <summary>
    ///     Tries to get unsaved changes information for display in dialogs.
    /// </summary>
    /// <param name="info">Output parameter with unsaved changes info including description and navigation. For multiple registrations, navigation action is empty.</param>
    /// <returns>True if there are unsaved changes, false otherwise.</returns>
    bool TryGetUnsavedChangesInfo(out UnsavedChangesInfo info);

    /// <summary>
    ///     Gets whether the main window is currently in the background (minimized or not active).
    /// </summary>
    bool IsInBackground { get; }
    
    /// <summary>
    ///     Requests the application to shut down gracefully.
    /// </summary>
    void Shutdown();

    /// <summary>
    ///     Brings the main window to the front and activates it.
    /// </summary>
    void BringToFront();

    /// <summary>
    ///     Shows a custom overlay asynchronously and waits for it to close.
    /// </summary>
    /// <param name="model">The overlay view model to display.</param>
    /// <returns>A task that completes when the overlay is dismissed.</returns>
    Task ShowOverlayAsync(OverlayModel model);
    
    /// <summary>
    ///     Shows a confirmation dialog and returns the user's response.
    /// </summary>
    /// <param name="message">The confirmation message with title, text, and button labels.</param>
    /// <returns>True if user confirmed (OK), false if cancelled.</returns>
    Task<bool> ShowOverlayAsync(ConfirmationMessage message);
    
    /// <summary>
    ///     Shows an information dialog and waits for user acknowledgment.
    /// </summary>
    /// <param name="message">The information message with title and text.</param>
    /// <returns>A task that completes when the user acknowledges the message.</returns>
    Task ShowOverlayAsync(InformationMessage message);
    
    /// <summary>
    ///     Shows a key phrase validation dialog requiring user to type a phrase to confirm a dangerous operation.
    /// </summary>
    /// <param name="message">The validation message with key phrase and instructions.</param>
    /// <returns>True if the user correctly typed the key phrase, false if cancelled.</returns>
    Task<bool> ShowOverlayAsync(KeyPhraseValidationMessage message);
    
    /// <summary>
    ///     Shows a busy overlay with progress indication.
    /// </summary>
    /// <param name="busyOverlay">The busy overlay view model with message and progress.</param>
    /// <returns>A task that completes when the overlay is dismissed.</returns>
    Task ShowBusyOverlayAsync(BusyOverlayViewModel busyOverlay);
}