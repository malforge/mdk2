using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Mdk.Hub.Features.Shell;

/// <summary>
/// A control that displays an animated starfield background with an avatar overlay.
/// </summary>
public class EasterEgg : Control
{
    const int AvatarHeight = 200;
    const int AvatarMargin = 20;
    readonly Random _random = new(42); // Fixed seed for consistent pattern
    readonly List<Star> _stars = new();
    readonly DispatcherTimer _timer;
    Bitmap? _avatar;
    Size _lastSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="EasterEgg"/> class.
    /// </summary>
    public EasterEgg()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(66) // ~15fps
        };
        _timer.Tick += (_, _) => InvalidateVisual();

        IsHitTestVisible = false;
    }

    /// <summary>
    /// Called when the control is attached to the visual tree.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        try
        {
            var assets = AssetLoader.Open(new Uri("avares://Mdk.Hub/Assets/space_engineers_black_256.png"));
            _avatar = new Bitmap(assets);
        }
        catch
        {
            // Avatar not found, continue without it
        }

        _timer.Start();
    }

    /// <summary>
    /// Called when the control is detached from the visual tree.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer.Stop();
    }

    /// <summary>
    /// Measures the desired size of the control.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The available size.</returns>
    protected override Size MeasureOverride(Size availableSize) => availableSize;

    /// <summary>
    /// Arranges the control and initializes stars if the size has changed.
    /// </summary>
    /// <param name="finalSize">The final size allocated to the control.</param>
    /// <returns>The final size used.</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_stars.Count == 0 || Math.Abs(_lastSize.Width - finalSize.Width) > 1 || Math.Abs(_lastSize.Height - finalSize.Height) > 1)
        {
            _lastSize = finalSize;
            InitializeStars(finalSize);
        }
        return base.ArrangeOverride(finalSize);
    }

    void InitializeStars(Size size)
    {
        _stars.Clear();

        if (size.Width < 100 || size.Height < 100)
            return;

        var starCount = (int)(size.Width * size.Height / 8000);

        // Calculate avatar exclusion zone (will be calculated based on actual avatar size if loaded)
        double avatarLeft = size.Width;
        double avatarTop = size.Height;
        double avatarRight = size.Width;
        double avatarBottom = size.Height;
        
        if (_avatar != null)
        {
            var aspectRatio = (double)_avatar.PixelSize.Width / _avatar.PixelSize.Height;
            var avatarWidth = AvatarHeight * aspectRatio;
            avatarLeft = size.Width - avatarWidth - AvatarMargin;
            avatarTop = size.Height - AvatarHeight - AvatarMargin;
            avatarRight = size.Width - AvatarMargin;
            avatarBottom = size.Height - AvatarMargin;
        }

        for (var i = 0; i < starCount; i++)
        {
            double x, y;
            var attempts = 0;

            // Keep trying until we find a position outside avatar area
            do
            {
                x = _random.NextDouble() * size.Width;
                y = _random.NextDouble() * size.Height;
                attempts++;
            } while (attempts < 10 && x >= avatarLeft && x <= avatarRight && y >= avatarTop && y <= avatarBottom);

            // Skip this star if we couldn't find a valid position
            if (x >= avatarLeft && x <= avatarRight && y >= avatarTop && y <= avatarBottom)
                continue;

            _stars.Add(new Star
            {
                X = x,
                Y = y,
                BaseOpacity = 0.3 + _random.NextDouble() * 0.5,
                TwinkleSpeed = 0.5 + _random.NextDouble() * 2.0,
                Size = 1.0 + _random.NextDouble() * 1.5
            });
        }
    }

    /// <summary>
    /// Renders the starfield and avatar overlay.
    /// </summary>
    /// <param name="context">The drawing context.</param>
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var time = DateTime.Now.TimeOfDay.TotalSeconds;

        // Draw stars
        foreach (var star in _stars)
        {
            var opacity = star.BaseOpacity * (0.5 + 0.5 * Math.Sin(time * star.TwinkleSpeed));
            var brush = new SolidColorBrush(Colors.White, opacity);
            context.DrawEllipse(brush, null, new Point(star.X, star.Y), star.Size, star.Size);
        }

        // Draw avatar in lower right corner
        if (_avatar != null)
        {
            var aspectRatio = (double)_avatar.PixelSize.Width / _avatar.PixelSize.Height;
            var avatarWidth = AvatarHeight * aspectRatio;
            
            var destRect = new Rect(
                Bounds.Width - avatarWidth - AvatarMargin,
                Bounds.Height - AvatarHeight - AvatarMargin,
                avatarWidth,
                AvatarHeight
            );

            using (context.PushOpacity(0.20))
                context.DrawImage(_avatar, destRect);
        }
    }

    class Star
    {
        public double X { get; init; }
        public double Y { get; init; }
        public double BaseOpacity { get; init; }
        public double TwinkleSpeed { get; init; }
        public double Size { get; init; }
    }
}
