using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Mdk.Hub.Features.Snackbars;

public partial class SnackbarWindow : Window
{
    public SnackbarWindow()
    {
        InitializeComponent();
        
        Opened += OnOpened;
        Closing += OnClosing;
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
    
    async void OnCloseRequested(object? sender, EventArgs e)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await FadeOutAsync();
            Close();
        });
    }
    
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
