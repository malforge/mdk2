using Avalonia;
using Avalonia.Controls;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Framework;

/// <summary>
///     Provides an attached property that exposes the DI container associated with a window,
///     allowing controls and behaviors to resolve services scoped to their host window.
/// </summary>
public static class WindowContainer
{
    /// <summary>
    ///     Attached property that holds the <see cref="IDependencyContainer" /> for a window.
    /// </summary>
    public static readonly AttachedProperty<IDependencyContainer?> ContainerProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, IDependencyContainer?>("Container", typeof(WindowContainer));

    /// <summary>Gets the container attached to the specified object.</summary>
    public static IDependencyContainer? GetContainer(AvaloniaObject obj) => obj.GetValue(ContainerProperty);

    /// <summary>Sets the container on the specified object.</summary>
    public static void SetContainer(AvaloniaObject obj, IDependencyContainer? value) => obj.SetValue(ContainerProperty, value);

    /// <summary>
    ///     Walks up to the <see cref="TopLevel" /> that hosts <paramref name="visual" /> and returns
    ///     the container registered on it, or <c>null</c> if none is registered.
    /// </summary>
    public static IDependencyContainer? GetContainerForVisual(Visual visual)
        => TopLevel.GetTopLevel(visual) is { } topLevel ? GetContainer(topLevel) : null;
}
