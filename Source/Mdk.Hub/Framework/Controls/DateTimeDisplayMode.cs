namespace Mdk.Hub.Framework.Controls;

/// <summary>
///     Defines the display modes for <see cref="DateTimeDisplay" /> control.
/// </summary>
public enum DateTimeDisplayMode
{
    /// <summary>
    ///     Display time as relative (e.g., "5 minutes ago", "2 days ago").
    /// </summary>
    Relative,

    /// <summary>
    ///     Display time in local timezone (e.g., "2024-01-25 09:15:30").
    /// </summary>
    Local,

    /// <summary>
    ///     Display time in UTC timezone (e.g., "2024-01-25 14:15:30 UTC").
    /// </summary>
    Utc
}