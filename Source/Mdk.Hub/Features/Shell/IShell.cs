using System;
using System.Collections.ObjectModel;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Shell;

public interface IShell
{
    /// <summary>
    ///     Gets the collection of active toast messages.
    /// </summary>
    ObservableCollection<ToastMessage> ToastMessages { get; }

    /// <summary>
    ///     Gets whether the easter egg should be visible.
    /// </summary>
    bool IsEasterEggActive { get; }

    void Start(string[] args);

    /// <summary>
    ///     Registers a callback to be invoked when Shell has started.
    ///     If already started, invokes immediately. Otherwise queues until Start() is called.
    /// </summary>
    void WhenStarted(Action<string[]> callback);

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
    ///     Disables the easter egg for today only.
    /// </summary>
    void DisableEasterEggForToday();

    /// <summary>
    ///     Disables the easter egg permanently.
    /// </summary>
    void DisableEasterEggForever();

    /// <summary>
    ///     Raised when the easter egg active state changes.
    /// </summary>
    event EventHandler? EasterEggActiveChanged;

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