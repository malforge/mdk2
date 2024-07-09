using System;

namespace Mal.DocumentGenerator.Common;

public class CommandLineException : Exception
{
    public CommandLineException() { }
    public CommandLineException(string message) : base(message) { }
    public CommandLineException(string message, Exception inner) : base(message, inner) { }
}