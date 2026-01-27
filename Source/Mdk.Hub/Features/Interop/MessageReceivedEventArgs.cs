using System;

namespace Mdk.Hub.Features.Interop;

/// <summary>
///     Event args for IPC message received events.
/// </summary>
public class MessageReceivedEventArgs : EventArgs
{
    public MessageReceivedEventArgs(InterConnectMessage message)
    {
        Message = message;
    }

    public InterConnectMessage Message { get; }
}