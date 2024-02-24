using System;

namespace Mdk.CommandLine.IngameScript.Api;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class RunAfterAttribute(Type type) : Attribute
{
    public Type Type { get; } = type;
}

public class RunAfterAttribute<T>() : RunAfterAttribute(typeof(T));