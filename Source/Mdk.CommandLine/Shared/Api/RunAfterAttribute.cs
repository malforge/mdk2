using System;

namespace Mdk.CommandLine.Shared.Api;

/// <summary>
/// Designates that the unit should after the other specified unit.
/// </summary>
/// <param name="type"></param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class RunAfterAttribute(Type type) : Attribute
{
    public Type Type { get; } = type;
}

/// <summary>
/// Designates that the unit should after the other specified unit.
/// </summary>
/// <typeparam name="T"></typeparam>
public class RunAfterAttribute<T>() : RunAfterAttribute(typeof(T));