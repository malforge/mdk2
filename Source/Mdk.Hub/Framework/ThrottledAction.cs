using System;
using System.Timers;
using Avalonia.Threading;

namespace Mdk.Hub.Framework;

/// <summary>
///     A throttled action that only executes after a certain delay since the last invocation. Any invocations
///     during the delay period will reset the timer.
/// </summary>
public class ThrottledAction<T>(Action<T> action, TimeSpan delay)
    where T : class
{
    readonly Action<T> _action = action ?? throw new ArgumentNullException(nameof(action));
    readonly TimeSpan _delay = delay;
    T? _lastParam;
    Timer? _timer;

    public void Invoke(T param)
    {
        _lastParam = param;
        if (_timer == null)
        {
            _timer = new Timer(_delay.TotalMilliseconds) { AutoReset = false };
            _timer.Elapsed += TimerElapsed;
        }
        else
            _timer.Stop();
        _timer.Start();
    }

    void TimerElapsed(object? sender, ElapsedEventArgs e)
    {
        var param = _lastParam;
        if (param != null)
            Dispatcher.UIThread.Post(() => ExecuteAction(param));
    }

    void ExecuteAction(T param) => _action(param);
}

/// <summary>
///     A throttled action that only executes after a certain delay since the last invocation. Any invocations
///     during the delay period will reset the timer.
/// </summary>
public class ThrottledAction(Action action, TimeSpan delay)
{
    readonly Action _action = action ?? throw new ArgumentNullException(nameof(action));
    readonly TimeSpan _delay = delay;
    Timer? _timer;

    public void Invoke()
    {
        if (_timer == null)
        {
            _timer = new Timer(_delay.TotalMilliseconds) { AutoReset = false };
            _timer.Elapsed += TimerElapsed;
        }
        else
            _timer.Stop();
        _timer.Start();
    }

    void TimerElapsed(object? sender, ElapsedEventArgs e) => Dispatcher.UIThread.Post(ExecuteAction);

    void ExecuteAction() => _action();
}