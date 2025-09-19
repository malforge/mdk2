using System.Diagnostics;

namespace Mdk.DocGen3;

public abstract class ContextBase
{
    int _lastPercent;
    int _max;
    string? _message;
    int _steps;
    Stopwatch? _stopwatch;
    Stopwatch? _totalStopwatch;

    public void BeginProgress(string message, int max)
    {
        _message = message;
        _steps = 0;
        _max = max;
        Console.WriteLine($"Starting: {_message}");
        Console.WriteLine("(0 %)");
        _stopwatch = Stopwatch.StartNew();
        _totalStopwatch = Stopwatch.StartNew();
    }

    public void Progress(int steps = 1, bool forceOutput = false)
    {
        if (_stopwatch == null)
            throw new InvalidOperationException("Progress tracking has not been started. Call BeginProgress first.");

        if (steps < 1) throw new ArgumentOutOfRangeException(nameof(steps), "Steps must be at least 1.");
        if (steps > _max) throw new ArgumentOutOfRangeException(nameof(steps), $"Steps cannot exceed the maximum value of {_max}.");

        _steps = Math.Clamp(_steps + steps, 0, _max - 1);
        var percent = (int)(_steps * 100.0 / _max);
        if ((_stopwatch.ElapsedMilliseconds > 1000 || forceOutput) && percent != _lastPercent)
        {
            _lastPercent = percent;
            Console.WriteLine($"({percent} %)");
        }
    }

    public void EndProgress()
    {
        if (_stopwatch == null)
            throw new InvalidOperationException("Progress tracking has not been started. Call BeginProgress first.");

        _stopwatch.Stop();
        _totalStopwatch?.Stop();
        if (_lastPercent < 100)
        {
            _lastPercent = 100;
            Console.WriteLine("(100 %)");
        }
        Console.WriteLine($"Finished: {_message} in {_totalStopwatch?.Elapsed.TotalSeconds:F2} seconds");
        _stopwatch = null;
    }
}