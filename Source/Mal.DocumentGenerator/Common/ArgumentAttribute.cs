using System;

namespace Mal.DocumentGenerator.Common;

/// <summary>
/// Declares that a property should be treated as an argument in a command line.
/// </summary>
public class ArgumentAttribute : Attribute
{
    /// <summary>
    /// Declares that a property should be treated as an argument in a command line.
    /// </summary>
    /// <param name="position"></param>
    public ArgumentAttribute(int position)
    {
        Position = position;
    }

    public int Position { get; }
    public bool Required { get; set; }
}