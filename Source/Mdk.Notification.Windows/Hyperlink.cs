using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Mdk.Notification.Windows;

public class Hyperlink : Button
{
    public static readonly DependencyProperty WasVisitedProperty = DependencyProperty.Register(
        nameof(WasVisited),
        typeof(bool),
        typeof(Hyperlink),
        new PropertyMetadata(default(bool)));

    public static readonly DependencyProperty ForegroundVisitedProperty = DependencyProperty.Register(
        nameof(ForegroundVisited),
        typeof(Brush),
        typeof(Hyperlink),
        new PropertyMetadata(default(Brush?)));

    static Hyperlink()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Hyperlink), new FrameworkPropertyMetadata(typeof(Hyperlink)));
    }

    public bool WasVisited
    {
        get => (bool)GetValue(WasVisitedProperty);
        set => SetValue(WasVisitedProperty, value);
    }

    public Brush? ForegroundVisited
    {
        get => (Brush?)GetValue(ForegroundVisitedProperty);
        set => SetValue(ForegroundVisitedProperty, value);
    }

    protected override void OnClick()
    {
        WasVisited = true;
        base.OnClick();
    }
}