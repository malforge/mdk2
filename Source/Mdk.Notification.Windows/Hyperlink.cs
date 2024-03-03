using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Mdk.Notification.Windows;

/// <summary>
///     A hyperlink control.
/// </summary>
public class Hyperlink : Button
{
    /// <summary>
    ///     Dependency property for <see cref="WasVisited" />.
    /// </summary>
    public static readonly DependencyProperty WasVisitedProperty = DependencyProperty.Register(
        nameof(WasVisited),
        typeof(bool),
        typeof(Hyperlink),
        new PropertyMetadata(default(bool)));

    /// <summary>
    ///     Dependency property for <see cref="ForegroundVisited" />.
    /// </summary>
    public static readonly DependencyProperty ForegroundVisitedProperty = DependencyProperty.Register(
        nameof(ForegroundVisited),
        typeof(Brush),
        typeof(Hyperlink),
        new PropertyMetadata(default(Brush?)));

    static Hyperlink()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Hyperlink), new FrameworkPropertyMetadata(typeof(Hyperlink)));
    }

    /// <summary>
    ///     Whether or not the hyperlink has been visited.
    /// </summary>
    public bool WasVisited
    {
        get => (bool)GetValue(WasVisitedProperty);
        set => SetValue(WasVisitedProperty, value);
    }

    /// <summary>
    ///     The foreground color to use when the hyperlink has been visited.
    /// </summary>
    public Brush? ForegroundVisited
    {
        get => (Brush?)GetValue(ForegroundVisitedProperty);
        set => SetValue(ForegroundVisitedProperty, value);
    }

    /// <inheritdoc />
    protected override void OnClick()
    {
        WasVisited = true;
        base.OnClick();
    }
}