using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Mdk.Notification.Components;
using Mdk.Notification.ViewModels;
using Mdk.Notification.Views;

namespace Mdk.Notification.Services;

public class Toasts
{
    const int Spacing = 8;

    readonly List<ToastWindow> _toastWindows = [];
    Task _pendingTask = Task.CompletedTask;

    public Toasts()
    {
        ToastWindows = new ReadOnlyCollection<ToastWindow>(_toastWindows);
    }

    public ReadOnlyCollection<ToastWindow> ToastWindows { get; }

    async Task<TaskCompletionSource> BeginOperationAsync()
    {
        var pendingTask = _pendingTask;
        var tcs = new TaskCompletionSource();
        _pendingTask = tcs.Task;
        await pendingTask;
        return tcs;
    }

    public async void ShowToast(Toast toast)
    {
        var tcs = await BeginOperationAsync();
        var toastWindow = new ToastWindow { DataContext = new ToastWindowViewModel(toast) };
        _toastWindows.Add(toastWindow);
        toast.Dismissed += (_, _) => toastWindow.Close();
        toastWindow.Closing += OnToastWindowOnClosing;

        async void onToastWindowOnLoaded(object? s, RoutedEventArgs _)
        {
            try
            {
                if (s is not ToastWindow w)
                    return;

                w.Loaded -= onToastWindowOnLoaded;
                await TransitionToastInAsync(w);
                tcs.TrySetResult();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        toastWindow.Loaded += onToastWindowOnLoaded;
        toastWindow.Show();
    }

    async void OnToastWindowOnClosing(object? s, WindowClosingEventArgs e)
    {
        try
        {
            if (s is not ToastWindow w)
                return;
            if (w.Tag is "Dead")
                return;

            e.Cancel = true;
            var tcs = await BeginOperationAsync();
            w.Tag = "Dead";
            _toastWindows.Remove(w);
            await Task.WhenAll(
                RearrangeToastsAsync(),
                TransitionToastOutAsync(w)
            );
            tcs.TrySetResult();
        }
        catch (Exception)
        {
            // ignored
        }
    }

    async Task TransitionToastInAsync(ToastWindow toastWindow)
    {
        var behavior = ToastBehavior.GetBehavior(toastWindow);
        var workingArea = behavior.GetWorkingArea();
        var bottom = workingArea.Bottom;
        var windowSize = toastWindow.FrameSize ?? new Size(0, 0);

        var totalHeight = (int)_toastWindows.Where(w => w != toastWindow).Sum(w => w.FrameSize?.Height ?? 0);
        totalHeight += Spacing * (_toastWindows.Count - 1);
        bottom -= totalHeight;
        var targetY = bottom - windowSize.Height;
        var sourceY = targetY + windowSize.Height;
        await Task.WhenAll(
            behavior.TransitionPositionAsync(sourceY, targetY),
            behavior.FadeInAsync()
        );
    }

    async Task TransitionToastOutAsync(ToastWindow toastWindow)
    {
        try
        {
            var behavior = ToastBehavior.GetBehavior(toastWindow);
            var frameSize = toastWindow.FrameSize ?? new Size(0, 0);
            var position = behavior.GetPosition();
            var targetY = position.Y + frameSize.Height;
            await Task.WhenAll(
                behavior.TransitionPositionAsync(targetY),
                behavior.FadeOutAsync()
            );
            toastWindow.Close();
        }
        catch (Exception)
        {
            toastWindow.Close();
        }
    }

    async Task RearrangeToastsAsync()
    {
        List<Task> transitionTasks = [];
        var totalHeight = 0.0;
        foreach (var toastWindow in _toastWindows)
        {
            var behavior = ToastBehavior.GetBehavior(toastWindow);
            var workingArea = behavior.GetWorkingArea();
            var bottom = workingArea.Bottom;
            var windowSize = toastWindow.FrameSize ?? new Size(0, 0);
            bottom -= totalHeight;
            var targetY = bottom - windowSize.Height;
            transitionTasks.Add(behavior.TransitionPositionAsync(targetY));
            
            totalHeight += windowSize.Height + Spacing;
        }

        await Task.WhenAll(transitionTasks);
    }
}