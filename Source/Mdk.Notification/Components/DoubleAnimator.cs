using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Animation.Easings;
using Avalonia.Threading;

namespace Mdk.Notification.Components;

public class DoubleAnimator : IDisposable
{
    readonly Action<double> _apply;
    Stopwatch _stopwatch = new();
    TaskCompletionSource? _tcs;
    DispatcherTimer _timer = new();
    double _value;

    public DoubleAnimator(Action<double> apply)
    {
        _apply = apply;
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick += OnAnimationTimer;
    }

    public Easing? Easing { get; set; }
    public TimeSpan Duration { get; set; }
    public double From { get; set; }
    public double To { get; set; }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnAnimationTimer;
        _timer = null!;
        _stopwatch = null!;
    }

    void OnAnimationTimer(object? sender, EventArgs e)
    {
        var isDone = false;
        var progress = _stopwatch.Elapsed / Duration;
        progress = Easing?.Ease(progress) ?? progress;
        if (progress >= 1)
        {
            progress = 1;
            StopCore();
            isDone = true;
        }

        _value = From + (To - From) * progress;
        _apply(_value);
        if (isDone)
            Complete();
    }

    void Complete()
    {
        _tcs?.TrySetResult();
        _tcs = null;
    }

    public void Start()
    {
        if (Duration == TimeSpan.Zero)
        {
            _timer.Stop();
            _stopwatch.Stop();
            _value = To;
            _apply(To);
            return;
        }
        _stopwatch.Restart();
        _timer.Start();
        Complete();
        _tcs = new TaskCompletionSource();
    }

    public async Task StartAsync()
    {
        Start();
        await (_tcs?.Task ?? Task.CompletedTask);
    }

    public void Stop()
    {
        StopCore();
        Complete();
    }

    void StopCore()
    {
        _timer.Stop();
        _stopwatch.Stop();
    }
}