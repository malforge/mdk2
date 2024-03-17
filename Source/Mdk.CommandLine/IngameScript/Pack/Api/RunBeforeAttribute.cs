using System;

namespace Mdk.CommandLine.IngameScript.Pack.Api;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class RunBeforeAttribute(Type type) : Attribute
{
    public Type Type { get; } = type;
}

public class RunBeforeAttribute<T>() : RunBeforeAttribute(typeof(T));