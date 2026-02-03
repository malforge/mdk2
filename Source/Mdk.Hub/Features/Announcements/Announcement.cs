using System;

namespace Mdk.Hub.Features.Announcements;

/// <summary>
///     Represents an announcement to display to users.
/// </summary>
public record Announcement
{
    /// <summary>
    ///     Unique identifier for this announcement. Used for dismissal tracking.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    ///     The announcement message text.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    ///     Visual style for the announcement card.
    /// </summary>
    public AnnouncementStyle Style { get; init; } = AnnouncementStyle.Info;

    /// <summary>
    ///     Optional link URL to open when user clicks the action button.
    /// </summary>
    public string? Link { get; init; }

    /// <summary>
    ///     Optional text for the action button. Only relevant if Link is provided.
    /// </summary>
    public string? LinkText { get; init; }

    /// <summary>
    ///     UTC datetime when this announcement should start showing. If not specified, shows immediately.
    /// </summary>
    public DateTime? ShowAfter { get; init; }

    /// <summary>
    ///     When this announcement was retrieved.
    /// </summary>
    public DateTime FetchedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
///     Visual style options for announcements.
/// </summary>
public enum AnnouncementStyle
{
    Info,
    Success,
    Danger
}

