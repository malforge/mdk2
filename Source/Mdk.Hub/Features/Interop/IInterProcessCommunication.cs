using System;
using System.Threading.Tasks;

namespace Mdk.Hub.Features.Interop;

/// <summary>
/// Interface for inter-process communication between CommandLine and Hub.
/// </summary>
public interface IInterProcessCommunication : IDisposable
{
    /// <summary>
    /// Event raised when a message is received from another process.
    /// </summary>
    event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// Determines whether the Hub is already running.
    /// </summary>
    bool IsAlreadyRunning();

    /// <summary>
    /// Submits a message to the running Hub instance (or to self if we are the server).
    /// </summary>
    Task SubmitAsync(InterConnectMessage message);
}
