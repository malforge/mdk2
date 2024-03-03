using System.Windows;
using System.Windows.Media.Animation;

namespace Mdk.Notification.Windows;

/// <summary>
///    Provides extension methods for window transitions.
/// </summary>
public static class Transitions
{
    /// <summary>
    ///    Slides the window in from the bottom edge of its current position.
    /// </summary>
    /// <param name="window"></param>
    public static async Task SlideInFromBelowAsync(this Window window)
    {
        // Stop any conflicting animations
        window.BeginAnimation(Window.TopProperty, null);
        window.BeginAnimation(UIElement.OpacityProperty, null);

        var contentSize = new Size(window.Width, window.Height);
        var topAnimation = new DoubleAnimation(window.Top + contentSize.Height, window.Top, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        
        var tcs = new TaskCompletionSource();
        void onCompleted(object? o, EventArgs eventArgs) => tcs.SetResult();
        fadeInAnimation.Completed += onCompleted;
        
        window.BeginAnimation(Window.TopProperty, topAnimation);
        window.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
        
        await tcs.Task.ConfigureAwait(false);
    }

    /// <summary>
    ///   Slides the window out over the bottom edge of its current position.
    /// </summary>
    /// <param name="window">The window to slide out.</param>
    /// <param name="close">Whether to close the window after the animation completes.</param>
    public static async Task SlideOutBelowAsync(this Window window, bool close = true)
    {
        // Stop any conflicting animations
        window.BeginAnimation(Window.TopProperty, null);
        window.BeginAnimation(UIElement.OpacityProperty, null);

        var contentSize = new Size(window.Width, window.Height);
        var topAnimation = new DoubleAnimation(window.Top, window.Top + contentSize.Height, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var tcs = new TaskCompletionSource();
        void onCompleted(object? o, EventArgs eventArgs)
        {
            if (close)
            {
                window.Close();
            }
            tcs.SetResult();
        }
        fadeOutAnimation.Completed += onCompleted;
        
        window.BeginAnimation(Window.TopProperty, topAnimation);
        window.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);

        await tcs.Task.ConfigureAwait(false);
    }
}