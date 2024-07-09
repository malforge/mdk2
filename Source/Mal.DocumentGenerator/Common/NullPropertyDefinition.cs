using System;

namespace Mal.DocumentGenerator.Common;

/// <summary>
///     A property definition that does nothing. Returned from <see cref="IConfigObjectDefinition" /> when a property is
///     not found.
/// </summary>
public sealed class NullPropertyDefinition : IConfigPropertyDefinition
{
    public static readonly NullPropertyDefinition Instance = new();
    NullPropertyDefinition() { }

    public bool Exists => false;
    public Type PropertyType => typeof(object);
    public string Category => string.Empty;
    public string Name => string.Empty;
    public string? Shorthand => null;
    public int? ArgumentPosition => null;
    public bool IsRequired => false;
    public string? DefaultImage => null;
    public object? DefaultValue => null;
    public string? Description => null;

    public string? GetImage(object instance) => null;
    public void SetImage(object instance, string? value) { }
    public object? GetValue(object instance) => null;
    public void SetValue(object instance, object? value) { }
}