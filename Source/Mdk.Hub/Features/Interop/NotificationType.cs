namespace Mdk.Hub.Features.Interop;

/// <summary>
///     Defines the type of notification received from CommandLine.
/// </summary>
public enum NotificationType
{
    /// <summary>
    ///     A custom notification message.
    /// </summary>
    Custom = 0,

    /// <summary>
    ///     A script build/deployment notification.
    /// </summary>
    Script = 1,

    /// <summary>
    ///     A mod build/deployment notification.
    /// </summary>
    Mod = 2,

    /// <summary>
    ///     A NuGet package version available notification.
    /// </summary>
    Nuget = 3,

    /// <summary>
    ///     Raw startup arguments forwarded from a secondary instance.
    /// </summary>
    StartupArgs = 4
}
