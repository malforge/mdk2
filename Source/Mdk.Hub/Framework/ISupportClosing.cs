using System.Threading.Tasks;

namespace Mdk.Hub.Framework;

/// <summary>
///     Interface for view models that need to handle window close events.
/// </summary>
public interface ISupportClosing
{
    /// <summary>
    ///     Called when the window is requested to close.
    /// </summary>
    /// <returns>True if the window can close; false to cancel the close operation.</returns>
    Task<bool> WillCloseAsync();

    /// <summary>
    /// Called after the window has been closed. Use this for any cleanup that needs to happen after the window is gone.
    /// </summary>
    /// <returns></returns>
    Task DidCloseAsync();
}