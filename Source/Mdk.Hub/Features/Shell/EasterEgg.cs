using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Mdk.Hub.Features.Shell;

public class EasterEgg : Control
{
    const int AvatarSize = 200;
    const int AvatarMargin = 20;
    readonly Random _random = new(42); // Fixed seed for consistent pattern
    readonly List<Star> _stars = new();
    readonly DispatcherTimer _timer;
    Bitmap? _avatar;
    Size _lastSize;
    IShell? _shell;

    public EasterEgg()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(66) // ~15fps
        };
        _timer.Tick += (_, _) => InvalidateVisual();

        IsHitTestVisible = false;
    }

    public void Initialize(IShell shell)
    {
        _shell = shell;
        _shell.EasterEggActiveChanged += OnEasterEggActiveChanged;
        UpdateVisibility();
    }

    void OnEasterEggActiveChanged(object? sender, EventArgs e) => UpdateVisibility();

    void UpdateVisibility() => IsVisible = _shell?.IsEasterEggActive ?? false;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        try
        {
            var assets = AssetLoader.Open(new Uri("avares://Mdk.Hub/Assets/malware256.png"));
            _avatar = new Bitmap(assets);
        }
        catch
        {
            // Avatar not found, continue without it
        }

        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer.Stop();
    }

    protected override Size MeasureOverride(Size availableSize) => availableSize;

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

        // Calculate avatar exclusion zone
        var avatarLeft = size.Width - AvatarSize - AvatarMargin;
        var avatarTop = size.Height - AvatarSize - AvatarMargin;
        var avatarRight = size.Width - AvatarMargin;
        var avatarBottom = size.Height - AvatarMargin;

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
            var destRect = new Rect(
                Bounds.Width - AvatarSize - AvatarMargin,
                Bounds.Height - AvatarSize - AvatarMargin,
                AvatarSize,
                AvatarSize
            );

            using (context.PushOpacity(0.08))
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