using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Mdk.Hub.Features.Shell;

public interface IShell
{
    void Start();
    void AddOverlay(OverlayModel model);
    
    /// <summary>
    /// Shows a non-blocking toast message that auto-dismisses.
    /// </summary>
    void ShowToast(string message, int durationMs = 3000);
    
    /// <summary>
    /// Gets the collection of active toast messages.
    /// </summary>
    ObservableCollection<ToastMessage> ToastMessages { get; }
    
    /// <summary>
    /// Raised when the main window gains focus.
    /// </summary>
    event EventHandler? WindowFocusGained;
    
    /// <summary>
    /// Gets whether the easter egg should be visible.
    /// </summary>
    bool IsEasterEggActive { get; }
    
    /// <summary>
    /// Disables the easter egg for today only.
    /// </summary>
    void DisableEasterEggForToday();
    
    /// <summary>
    /// Disables the easter egg permanently.
    /// </summary>
    void DisableEasterEggForever();
    
    /// <summary>
    /// Raised when the easter egg active state changes.
    /// </summary>
    event EventHandler? EasterEggActiveChanged;
    
    /// <summary>
    /// Checks if there are any projects with unsaved changes.
    /// </summary>
    bool HasUnsavedChanges();
    
    /// <summary>
    /// Gets the first project path with unsaved changes, or null if none.
    /// </summary>
    string? GetFirstProjectWithUnsavedChanges();
    
    /// <summary>
    /// Registers or updates unsaved changes state for a project.
    /// </summary>
    void SetProjectUnsavedState(string projectPath, bool hasUnsavedChanges);
}

public partial class ToastMessage : ObservableObject
{
    public string Message { get; set; } = string.Empty;
    
    [ObservableProperty]
    bool _isDismissing;
}