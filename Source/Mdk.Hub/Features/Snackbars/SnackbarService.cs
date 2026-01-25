using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Mal.DependencyInjection;

namespace Mdk.Hub.Features.Snackbars;

/// <summary>
/// Service for displaying snackbar notifications.
/// </summary>
[Dependency<ISnackbarService>]
public class SnackbarService : ISnackbarService
{
    readonly List<SnackbarWindow> _activeSnackbars = new();
    readonly object _lock = new();
    
    Window? _mainWindow;

    public void SetMainWindow(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public void Show(string message, int timeout = 15000)
    {
        Show(message, Array.Empty<SnackbarAction>(), timeout);
    }

    public void Show(string message, IReadOnlyList<SnackbarAction> actions, int timeout = 15000)
    {
        _ = Dispatcher.UIThread.InvokeAsync(() => ShowAsync(message, actions, timeout));
    }

    async Task ShowAsync(string message, IReadOnlyList<SnackbarAction> actions, int timeout)
    {
        var viewModel = new SnackbarViewModel
        {
            Message = message,
            Timeout = timeout
        };
        viewModel.SetActions(actions);
        
        var snackbar = new SnackbarWindow
        {
            DataContext = viewModel
        };
        
        // Show the window first (invisible) so it can measure itself
        snackbar.Opacity = 0;
        snackbar.Show();
        
        // Wait a frame for layout to complete
        await Task.Delay(50);
        
        // Get the screen where the main window is (or primary if no main window)
        var screen = _mainWindow?.Screens.ScreenFromWindow(_mainWindow) 
                     ?? snackbar.Screens.Primary 
                     ?? snackbar.Screens.All.FirstOrDefault();
        
        if (screen == null)
        {
            snackbar.Close();
            return;
        }

        var workingArea = screen.WorkingArea;
        var scaling = screen.Scaling;
        
        // Calculate position - snackbar.Bounds gives logical size, need physical for calculations
        var snackbarWidth = snackbar.Bounds.Width * scaling;
        var snackbarHeight = snackbar.Bounds.Height * scaling;
        
        lock (_lock)
        {
            // Stack snackbars from bottom up - calculate in physical pixels
            var totalHeight = _activeSnackbars.Sum(s => s.Bounds.Height * scaling + 10 * scaling);
            
            // Center horizontally, position at bottom - all in physical pixels
            var x = (int)((workingArea.Width - snackbarWidth) / 2 + workingArea.X);
            var y = (int)(workingArea.Bottom - snackbarHeight - totalHeight - 20 * scaling);
            
            snackbar.Position = new PixelPoint(x, y);
            _activeSnackbars.Add(snackbar);
        }
        
        // Now trigger the fade-in by setting opacity back to 1
        snackbar.Opacity = 1;
        
        // Handle closing
        snackbar.Closed += (_, _) =>
        {
            lock (_lock)
            {
                _activeSnackbars.Remove(snackbar);
            }
            
            // Rearrange remaining snackbars
            Dispatcher.UIThread.InvokeAsync(RearrangeSnackbars);
        };
    }

    async void RearrangeSnackbars()
    {
        List<SnackbarWindow> snackbars;
        lock (_lock)
        {
            snackbars = _activeSnackbars.ToList();
        }

        if (snackbars.Count == 0)
            return;

        // Use the same screen as main window
        var screen = _mainWindow?.Screens.ScreenFromWindow(_mainWindow) 
                     ?? snackbars[0].Screens.Primary 
                     ?? snackbars[0].Screens.All.FirstOrDefault();
        if (screen == null)
            return;

        var workingArea = screen.WorkingArea;
        var scaling = screen.Scaling;
        
        // Calculate in physical pixels
        var bottom = workingArea.Bottom;

        var animations = new List<Task>();
        
        foreach (var snackbar in snackbars)
        {
            var snackbarHeightPhysical = snackbar.Bounds.Height * scaling;
            var desiredY = (int)(bottom - snackbarHeightPhysical - 20 * scaling);
            bottom = desiredY;
            
            // Animate the position change
            animations.Add(AnimatePositionAsync(snackbar, snackbar.Position.X, desiredY));
        }
        
        await Task.WhenAll(animations);
    }
    
    async Task AnimatePositionAsync(SnackbarWindow snackbar, int targetX, int targetY)
    {
        var startY = snackbar.Position.Y;
        var distance = targetY - startY;
        
        if (Math.Abs(distance) < 1)
            return; // Already at target
        
        var duration = 200; // milliseconds
        var steps = 20;
        var delay = duration / steps;
        
        for (int i = 1; i <= steps; i++)
        {
            var progress = (double)i / steps;
            // Ease out cubic
            var eased = 1 - Math.Pow(1 - progress, 3);
            var newY = (int)(startY + distance * eased);
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                snackbar.Position = new PixelPoint(targetX, newY);
            });
            
            await Task.Delay(delay);
        }
        
        // Ensure final position is exact
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            snackbar.Position = new PixelPoint(targetX, targetY);
        });
    }
}
