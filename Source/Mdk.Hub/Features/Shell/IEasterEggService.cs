using System;

namespace Mdk.Hub.Features.Shell;

/// <summary>
///     Service for managing the easter egg feature state.
/// </summary>
public interface IEasterEggService
{
    /// <summary>
    ///     Gets whether the easter egg should be active and displayed.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    ///     Raised when the easter egg active state changes.
    /// </summary>
    event EventHandler? ActiveChanged;

    /// <summary>
    ///     Disables the easter egg for a specified duration.
    /// </summary>
    /// <param name="duration">The duration to disable the easter egg for.</param>
    void DisableFor(TimeSpan duration);

    /// <summary>
    ///     Disables the easter egg permanently (until application restart).
    /// </summary>
    void DisableForever();
}

