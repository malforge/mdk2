using System;
using Avalonia.Controls;

namespace Mdk.Hub.Features.Projects.Overview.Icons;

/// <summary>
///     A custom control that displays symbols from the icon set.
/// </summary>
public class Symbol : ContentControl
{
    /// <summary>
    ///     Gets the style key override for this control.
    /// </summary>
    protected override Type StyleKeyOverride => typeof(Symbol);
}
