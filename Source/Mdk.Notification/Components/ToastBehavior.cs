using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;

namespace Mdk.Notification.Components;

public class ToastBehavior : AvaloniaObject, IDisposable
{
    static readonly AttachedProperty<ToastBehavior?> BehaviorProperty
        = AvaloniaProperty.RegisterAttached<ToastBehavior, AvaloniaObject, ToastBehavior?>("Behavior");

    readonly DoubleAnimator _opacityAnimator;
    readonly Window _window;
    readonly DoubleAnimator _yAnimator;

    public ToastBehavior(AvaloniaObject target)
    {
        _window = (Window)target;
        _yAnimator = new DoubleAnimator(AnimateYPosition);
        _yAnimator.Easing = new CubicEaseInOut();
        _opacityAnimator = new DoubleAnimator(value => _window.Opacity = value);
        _opacityAnimator.Easing = new CubicEaseInOut();
    }

    public void Dispose() { }

    public static ToastBehavior GetBehavior(AvaloniaObject target)
    {
        var behavior = target.GetValue(BehaviorProperty);
        if (behavior is null)
        {
            behavior = new ToastBehavior(target);
            target.SetValue(BehaviorProperty, behavior);
        }
        return behavior;
    }

    void AnimateYPosition(double value)
    {
        var screen = _window.Screens.Primary;
        if (screen is null)
            return;

        var screenSize = screen.WorkingArea.Size;
        var windowSize = PixelSize.FromSize(new Size(_window.Bounds.Width, _window.Bounds.Height), screen.Scaling);
        var newY = (int)Math.Round(value);

        _window.WindowStartupLocation = WindowStartupLocation.Manual;
        _window.Position = new PixelPoint(
            (screenSize.Width - windowSize.Width) / 2,
            newY);
    }

    public Point GetPosition()
    {
        var screen = _window.Screens.Primary;
        if (screen is null)
            return new Point();
        var scaling = screen.Scaling;

        var screenPosition = _window.Position;
        return new Point(
            screenPosition.X / scaling,
            screenPosition.Y / scaling);
    }

    public Rect GetWorkingArea()
    {
        var screen = _window.Screens.Primary;
        if (screen is null)
            return new Rect();
        var scaling = screen.Scaling;

        var screenWorkingArea = screen.WorkingArea;
        return new Rect(
            screenWorkingArea.X / scaling,
            screenWorkingArea.Y / scaling,
            screenWorkingArea.Width / scaling,
            screenWorkingArea.Height / scaling);
    }

    public async Task TransitionPositionAsync(double fromY, double toY)
    {
        if (Design.IsDesignMode)
            return;

        var scaling = _window.Screens.Primary!.Scaling;

        _yAnimator.Stop();
        _yAnimator.From = fromY * scaling;
        _yAnimator.To = toY * scaling;
        _yAnimator.Duration = TimeSpan.FromMilliseconds(300);
        await _yAnimator.StartAsync();
    }

    public Task TransitionPositionAsync(double toY) => TransitionPositionAsync(_window.Position.Y / _window.Screens.Primary!.Scaling, toY);

    public async Task FadeInAsync()
    {
        if (Design.IsDesignMode)
            return;

        _opacityAnimator.Stop();
        _opacityAnimator.From = _window.Opacity;
        _opacityAnimator.To = 1;
        _opacityAnimator.Duration = TimeSpan.FromMilliseconds((1 - _window.Opacity) * 300);
        await _opacityAnimator.StartAsync();
    }

    public async Task FadeOutAsync()
    {
        if (Design.IsDesignMode)
            return;

        _opacityAnimator.Stop();
        _opacityAnimator.From = _window.Opacity;
        _opacityAnimator.To = 0;
        _opacityAnimator.Duration = TimeSpan.FromMilliseconds(_window.Opacity * 300);
        await _opacityAnimator.StartAsync();
    }
}