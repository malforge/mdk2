namespace Mdk.Hub.Features.Interop;

/// <summary>
///     Defines the type of notification received from CommandLine.
/// </summary>
public enum NotificationType
{
    /// <summary>
    ///     A custom notification message.
    /// </summary>
    Custom,

    /// <summary>
    ///     A script build/deployment notification.
    /// </summary>
    Script,

    /// <summary>
    ///     A mod build/deployment notification.
    /// </summary>
    Mod,

    /// <summary>
    ///     A NuGet package version available notification.
    /// </summary>
    Nuget
}
