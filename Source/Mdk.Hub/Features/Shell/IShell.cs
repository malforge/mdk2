using System;

namespace Mdk.Hub.Features.Shell;

public interface IShell
{
    void Start();
    void AddOverlay(OverlayModel model);
    
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
}