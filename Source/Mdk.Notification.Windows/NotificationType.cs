namespace Mdk.Notification.Windows;

/// <summary>
/// Defines the type of the notification.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// The notification is a simple custom notification.
    /// </summary>
    Custom,
    
    /// <summary>
    /// The notification is a script publication notification.
    /// </summary>
    Script,
    
    /// <summary>
    /// The notification is a NuGet package version available notification.
    /// </summary>
    Nuget
}