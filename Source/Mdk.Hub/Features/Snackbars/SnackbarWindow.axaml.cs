using System;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Mdk.Hub.Features.Snackbars;

/// <summary>
///     Window displaying temporary notification messages (snackbars/toasts).
/// </summary>
public partial class SnackbarWindow : Window
{
    /// <summary>
    ///     Initializes a new instance of the SnackbarWindow class.
    /// </summary>
    public SnackbarWindow()
    {
        InitializeComponent();

        Opened += OnOpened;
        Closing += OnClosing;
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
    }

    async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is SnackbarViewModel vm)
        {
            vm.CloseRequested += OnCloseRequested;
            vm.StartTimeout();
        }

        // Fade in animation
        await FadeInAsync();
    }

    void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is SnackbarViewModel vm)
        {
            vm.CloseRequested -= OnCloseRequested;
            vm.CancelTimeout();
        }
    }

    void OnPointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (DataContext is SnackbarViewModel vm)
            vm.PauseTimeout();
    }

    void OnPointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (DataContext is SnackbarViewModel vm)
            vm.ResumeTimeout();
    }

    async void OnCloseRequested(object? sender, EventArgs e) =>
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await FadeOutAsync();
            Close();
        });

    async Task FadeInAsync()
    {
        Opacity = 0;

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(300),
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters = { new Setter { Property = OpacityProperty, Value = 0.0 } }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters = { new Setter { Property = OpacityProperty, Value = 1.0 } }
                }
            }
        };

        await animation.RunAsync(this);
    }

    async Task FadeOutAsync()
    {
        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(200),
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters = { new Setter { Property = OpacityProperty, Value = 1.0 } }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters = { new Setter { Property = OpacityProperty, Value = 0.0 } }
                }
            }
        };

        await animation.RunAsync(this);
    }
}
