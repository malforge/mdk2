using System.Threading.Tasks;

namespace Mdk.Hub.Framework;

/// <summary>
///     Implemented by view models that need to participate in their host window's close lifecycle.
/// </summary>
public interface ISupportClosing
{
    /// <summary>
    ///     Called before the window closes. Return <c>true</c> to allow the close, <c>false</c> to cancel.
    /// </summary>
    Task<bool> WillCloseAsync();

    /// <summary>Called after the window has closed.</summary>
    Task DidCloseAsync();
}
