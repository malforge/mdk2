using System;
using System.Threading.Tasks;

namespace Mdk.Hub.Features.Announcements;

/// <summary>
///     Service for fetching and managing user announcements.
/// </summary>
public interface IAnnouncementService
{
    /// <summary>
    ///     The last fetched announcement, if any.
    /// </summary>
    Announcement? LastKnownAnnouncement { get; }

    /// <summary>
    ///     Raised when a new announcement becomes available or the current announcement changes.
    /// </summary>
    event EventHandler<Announcement>? AnnouncementChanged;

    /// <summary>
    ///     Registers a callback to be invoked when an announcement is available.
    ///     If an announcement is already cached, invokes immediately.
    /// </summary>
    void WhenAnnouncementAvailable(Action<Announcement> callback);

    /// <summary>
    ///     Checks for announcements. Returns true if check completed successfully.
    /// </summary>
    Task<bool> CheckForAnnouncementsAsync();

    /// <summary>
    ///     Forces a fresh check for announcements, ignoring cache.
    /// </summary>
    Task<bool> ForceCheckAsync();

    /// <summary>
    ///     Marks an announcement as dismissed by the user.
    /// </summary>
    void DismissAnnouncement(string announcementId);

    /// <summary>
    ///     Checks if an announcement has been dismissed.
    /// </summary>
    bool IsAnnouncementDismissed(string announcementId);
}

