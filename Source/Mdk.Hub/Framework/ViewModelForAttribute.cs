using System;

namespace Mdk.Hub.Framework;

/// <summary>
///     Marks a viewmodel as being for a specific view.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public abstract class ViewModelForAttribute(Type viewType) : Attribute
{
    /// <summary>
    ///     The type of the view this viewmodel is for.
    /// </summary>
    public Type ViewType { get; } = viewType ?? throw new ArgumentNullException(nameof(viewType));
}

/// <summary>
///     Marks a viewmodel as being for a specific view.
/// </summary>
/// <typeparam name="T"></typeparam>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ViewModelForAttribute<T>() : ViewModelForAttribute(typeof(T))
    where T : class;
