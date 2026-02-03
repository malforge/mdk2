namespace Mdk.Hub.Features.Projects.Overview;

/// <summary>
///     How the user wants to interact with build notifications.
/// </summary>
public enum InteractiveMode
{
    /// <summary>
    ///     Open the Hub window when a build completes.
    /// </summary>
    OpenHub,
    
    /// <summary>
    ///     Show a toast notification when a build completes.
    /// </summary>
    ShowNotification,
    
    /// <summary>
    ///     Do not show any notification when a build completes.
    /// </summary>
    DoNothing
}

