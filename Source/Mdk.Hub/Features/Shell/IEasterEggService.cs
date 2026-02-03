using System;

namespace Mdk.Hub.Features.Shell;

public interface IEasterEggService
{
    /// <summary>
    /// Gets whether the easter egg should be active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Raised when the active state changes.
    /// </summary>
    event EventHandler? ActiveChanged;

    /// <summary>
    /// Disables the easter egg for a specified duration.
    /// </summary>
    void DisableFor(TimeSpan duration);

    /// <summary>
    /// Disables the easter egg permanently (until app restart).
    /// </summary>
    void DisableForever();
}

