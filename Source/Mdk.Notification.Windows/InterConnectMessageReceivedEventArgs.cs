namespace Mdk.Notification.Windows;

/// <summary>
/// Event arguments for when an interconnect message is received.
/// </summary>
/// <param name="message"></param>
public class InterConnectMessageReceivedEventArgs(string message) : EventArgs
{
    /// <summary>
    /// The message that was received.
    /// </summary>
    public string Message { get; } = message;
}