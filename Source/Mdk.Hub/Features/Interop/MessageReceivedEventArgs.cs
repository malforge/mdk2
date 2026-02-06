using System;

namespace Mdk.Hub.Features.Interop;

/// <summary>
///     Event args for IPC message received events.
/// </summary>
public class MessageReceivedEventArgs : EventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageReceivedEventArgs"/> class.
    /// </summary>
    /// <param name="message">The message that was received.</param>
    public MessageReceivedEventArgs(InterConnectMessage message)
    {
        Message = message;
    }

    /// <summary>
    ///     Gets the message that was received.
    /// </summary>
    public InterConnectMessage Message { get; }
}
