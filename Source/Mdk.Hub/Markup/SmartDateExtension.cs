using System;
using Avalonia.Markup.Xaml;

namespace Mdk.Hub.Markup;

/// <summary>
///     A markup extension that provides a smart date converter for XAML bindings.
/// </summary>
public class SmartDateExtension : MarkupExtension
{
    /// <summary>
    ///     Provides the value (a SmartDateConverter instance) for the markup extension.
    /// </summary>
    /// <param name="serviceProvider">Service provider for the markup extension.</param>
    /// <returns>A new SmartDateConverter instance.</returns>
    public override object ProvideValue(IServiceProvider serviceProvider) => new SmartDateConverter();
}