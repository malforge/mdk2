using System;

namespace Mdk.CommandLine.IngameScript.Pack.Api;

/// <summary>
///     Designates that the unit should run before the other specified unit.
/// </summary>
/// <param name="type"></param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class RunBeforeAttribute(Type type) : Attribute
{
    public Type Type { get; } = type;
}

/// <summary>
///     Designates that the unit should run before the other specified unit.
/// </summary>
/// <typeparam name="T"></typeparam>
public class RunBeforeAttribute<T>() : RunBeforeAttribute(typeof(T));