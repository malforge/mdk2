using System;

namespace Mdk.CommandLine;

/// <summary>
///     An exception that is thrown when an error occurs in the program, and the program should exit with an error code.
/// </summary>
public class CommandLineException : Exception
{
    /// <summary>
    ///     Creates a new instance of <see cref="CommandLineException" />.
    /// </summary>
    /// <param name="errorCode"></param>
    /// <param name="message"></param>
    public CommandLineException(int errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    //Creates a new instance of <see cref="CommandLineException"/>.
    public CommandLineException(int errorCode, string message, Exception inner) : base(message, inner)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    ///     The error code that should be returned when the program exits.
    /// </summary>
    public int ErrorCode { get; }
}