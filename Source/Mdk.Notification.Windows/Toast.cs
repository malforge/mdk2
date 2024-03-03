using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;

namespace Mdk.Notification.Windows;

/// <summary>
///     A class for showing toast notifications.
/// </summary>
public class Toast
{
    const int DefaultTimeout = 30000;
    static readonly Toast Instance = new();

    readonly Task _currentTask = Task.CompletedTask;
    readonly List<ToastWindow> _liveNotifications = new();

    public static bool IsEmpty => Instance._liveNotifications.Count == 0;

    /// <summary>
    ///     Shows a toast notification with the specified message and actions.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="actions"></param>
    /// <param name="timeout"></param>
    public static void Show(string message, IReadOnlyList<ToastAction>? actions = null, int timeout = DefaultTimeout) => Instance.ShowOne(message, actions, timeout);

    /// <summary>
    ///     Shows a toast notification with the specified message and actions.
    /// </summary>
    /// <param name="message">The message to display in the notification.</param>
    /// <param name="actions">Actions the user can take on the notification.</param>
    public static void Show(string message, params ToastAction[] actions) => Show(message, DefaultTimeout, actions);

    /// <summary>
    ///     Shows a toast notification with the specified message and actions.
    /// </summary>
    /// <param name="message">The message to display in the notification.</param>
    /// <param name="timeout">A timeout in milliseconds after which the notification will automatically close.</param>
    /// <param name="actions">Actions the user can take on the notification.</param>
    public static void Show(string message, int timeout, params ToastAction[] actions) => Show(message, actions, timeout);

    public static event EventHandler? Emptied;

    void ShowOne(string message, IReadOnlyList<ToastAction>? actions, int timeout) => _currentTask.ContinueWith(_ => ShowOneAsync(message, actions, timeout), TaskScheduler.FromCurrentSynchronizationContext());

    async Task ShowOneAsync(string message, IReadOnlyList<ToastAction>? actions, int timeout)
    {
        await Task.Yield();
        var notification = new ToastWindow
        {
            DataContext = new ToastViewModel
            {
                Message = message,
                Actions = actions ?? Array.Empty<ToastAction>()
            }
        };

        async void beginClosing(ToastWindow n)
        {
            await n.SlideOutBelowAsync();
        }

        CancellationTokenSource cts = new();

        void onNotificationOnClosing(object? sender, CancelEventArgs args)
        {
            if (sender is not ToastWindow n) return;
            if (!_liveNotifications.Contains(n))
                return;

            cts.Cancel();
            args.Cancel = true;
            beginClosing(notification);
            _liveNotifications.Remove(notification);
            _currentTask.ContinueWith(_ => RearrangeAsync(), TaskScheduler.FromCurrentSynchronizationContext());

            if (_liveNotifications.Count == 0)
                Emptied?.Invoke(this, EventArgs.Empty);
        }

        notification.Closing += onNotificationOnClosing;
        notification.Opacity = 0;
        var tcs = new TaskCompletionSource();

        void onLoaded(object? sender, RoutedEventArgs args)
        {
            notification.Loaded -= onLoaded;
            tcs.SetResult();
        }

        notification.Loaded += onLoaded;
        notification.Show();
        await tcs.Task;
        var bottom = _liveNotifications.Aggregate(SystemParameters.WorkArea.Bottom, (current, t) => current - (t.Height + 10));
        notification.Top = bottom - notification.Height - 10;
        notification.Left = (SystemParameters.WorkArea.Width - notification.Width) / 2;
        _liveNotifications.Add(notification);
        await notification.SlideInFromBelowAsync();
        if (timeout > 0)
        {
            await Task.Delay(timeout, cts.Token);
            if (!cts.Token.IsCancellationRequested)
                notification.Close();
        }
    }

    async Task RearrangeAsync()
    {
        var bottom = SystemParameters.WorkArea.Bottom;
        var desiredTops = new List<double>();
        foreach (var t in _liveNotifications)
        {
            var desiredTop = bottom - t.Height - 10;
            bottom = desiredTop;
            desiredTops.Add(desiredTop);
        }

        List<Task> tasks = new();
        for (var i = 0; i < _liveNotifications.Count; i++)
        {
            var existingNotification = _liveNotifications[i];
            var desiredTop = desiredTops[i];
            if (Math.Abs(existingNotification.Top - desiredTop) < 0.1)
                continue;

            var animation = new DoubleAnimation(desiredTop, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            var tcs = new TaskCompletionSource();

            void onCompleted(object? sender, EventArgs args)
            {
                tcs.SetResult();
            }

            animation.Completed += onCompleted;
            existingNotification.BeginAnimation(Window.TopProperty, animation);

            tasks.Add(tcs.Task);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}