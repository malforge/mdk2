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
}