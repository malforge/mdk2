using System;

namespace Mdk.Hub.Features.Interop;

/// <summary>
///     Provides methods for working with notification commands from the command line.
/// </summary>
public static class NotificationCommand
{
    /// <summary>
    ///     Determines whether the command-line arguments represent a notification command.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>True if the arguments represent a notification command; otherwise, false.</returns>
    public static bool IsNotificationCommand(string[] args)
    {
        if (args.Length < 3)
            return false;
        
        var command = args[0];
        return Enum.TryParse<NotificationType>(command, ignoreCase: true, out _);
    }
}
