using System;

namespace Mal.DocumentGenerator.Common;

/// <summary>
///     Associates a shorthand name with a property.
/// </summary>
public class ShorthandAttribute : Attribute
{
    /// <summary>
    ///     Associates a shorthand name with a property.
    /// </summary>
    /// <param name="name"></param>
    public ShorthandAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    ///     The shorthand name.
    /// </summary>
    public string Name { get; }
}