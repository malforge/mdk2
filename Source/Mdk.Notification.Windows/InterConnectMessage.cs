using System.IO;
using System.Text;

namespace Mdk.Notification.Windows;

/// <summary>
///     A message to be sent between different instances of the same application.
/// </summary>
/// <param name="type"></param>
/// <param name="arguments"></param>
public sealed class InterConnectMessage(NotificationType type, params string[] arguments)
{
    /// <summary>
    ///     The type of notification to show.
    /// </summary>
    public NotificationType Type { get; } = type;

    /// <summary>
    ///     Arguments to be passed to the notification.
    /// </summary>
    public string[] Arguments { get; } = arguments;

    /// <summary>
    ///     Reads a message from a stream.
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static InterConnectMessage Read(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);
        var type = (NotificationType)reader.ReadInt32();
        var arguments = new string[reader.ReadInt32()];
        for (var i = 0; i < arguments.Length; i++)
            arguments[i] = reader.ReadString();
        return new InterConnectMessage(type, arguments);
    }

    /// <summary>
    ///     Writes the message to a stream.
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public Task WriteAsync(Stream stream)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write((int)Type);
        writer.Write(Arguments.Length);
        foreach (var argument in Arguments)
            writer.Write(argument);
        writer.Flush();
        return Task.CompletedTask;
    }
}