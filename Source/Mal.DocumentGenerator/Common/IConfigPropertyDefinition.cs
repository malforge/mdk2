using System;

namespace Mal.DocumentGenerator.Common;

public interface IConfigPropertyDefinition
{
    /// <summary>
    ///     Determines if the property exists.
    /// </summary>
    bool Exists { get; }

    /// <summary>
    ///     The type of the property.
    /// </summary>
    Type PropertyType { get; }

    /// <summary>
    ///     What .ini category the property belongs to.
    /// </summary>
    string Category { get; }

    /// <summary>
    ///     The name of the property as it appears in the .ini file or command line arguments.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     An optional shorthand name for the property that can be used in command line arguments.
    /// </summary>
    string? Shorthand { get; }

    /// <summary>
    ///     If set, this property is an argument in a command line, and this is the position of the argument.
    /// </summary>
    int? ArgumentPosition { get; }

    /// <summary>
    ///     If set, this property is an argument in a command line, and it is required.
    /// </summary>
    bool IsRequired { get; }

    /// <summary>
    ///     The default value of the property as it would appear as a string.
    /// </summary>
    string? DefaultImage { get; }

    /// <summary>
    ///     The default value of the property.
    /// </summary>
    object? DefaultValue { get; }

    /// <summary>
    ///     A description of the property as it would appear in a help message.
    /// </summary>
    string? Description { get; }

    /// <summary>
    ///     Gets the string representation of the property value from the provided instance.
    /// </summary>
    /// <param name="instance"></param>
    /// <returns></returns>
    string? GetImage(object instance);

    /// <summary>
    ///     Sets the property value on the provided instance from the string representation.
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="value"></param>
    void SetImage(object instance, string? value);

    /// <summary>
    ///     Gets the property value from the provided instance.
    /// </summary>
    /// <param name="instance"></param>
    /// <returns></returns>
    object? GetValue(object instance);

    /// <summary>
    ///     Sets the property value on the provided instance.
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="value"></param>
    void SetValue(object instance, object? value);
}

public interface IConfigPropertyDefinition<T> : IConfigPropertyDefinition
{
    new T GetValue(object instance);
    void SetValue(object instance, T value);
}