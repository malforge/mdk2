using System.IO;
using System.Text;

namespace Mdk.Hub.Features.Interop;

/// <summary>
///     A message sent between MDK CommandLine and Hub via IPC.
/// </summary>
public sealed class InterConnectMessage(NotificationType type, params string[] arguments)
{
    /// <summary>
    ///     The type of notification.
    /// </summary>
    public NotificationType Type { get; } = type;

    /// <summary>
    ///     Arguments passed with the notification.
    /// </summary>
    public string[] Arguments { get; } = arguments;

    /// <summary>
    ///     Reads a message from a stream.
    /// </summary>
    public static InterConnectMessage Read(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);
        var type = (NotificationType)reader.ReadInt32();
        var argumentCount = reader.ReadInt32();
        var arguments = new string[argumentCount];

        for (var i = 0; i < argumentCount; i++)
            arguments[i] = reader.ReadString();

        return new InterConnectMessage(type, arguments);
    }

    /// <summary>
    ///     Writes the message to a stream.
    /// </summary>
    public void Write(Stream stream)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write((int)Type);
        writer.Write(Arguments.Length);

        foreach (var argument in Arguments)
            writer.Write(argument);

        writer.Flush();
    }
}
