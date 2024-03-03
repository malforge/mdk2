using System.Windows;

namespace Mdk.Notification.Windows;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    const int EmptyGracePeriod = 1000;

    async void OnNotificationsEmptied(object? sender, EventArgs e)
    {
        await Task.Delay(EmptyGracePeriod);
        if (Toast.IsEmpty)
            Shutdown();
    }

    void App_OnStartup(object sender, StartupEventArgs e)
    {
        Toast.Emptied += OnNotificationsEmptied;

        Toast.Show("Hello, world!",
            5000,
            new ToastAction
            {
                Text = "Click me!",
                Action = () => MessageBox.Show("You clicked me!")
            },
            new ToastAction
            {
                Text = "Click me too!",
                Action = () => MessageBox.Show("You clicked me too!"),
                IsClosingAction = true
            });
        Toast.Show("Hello, world!",
            5000,
            new ToastAction
            {
                Text = "Click me!",
                Action = () => MessageBox.Show("You clicked me!")
            },
            new ToastAction
            {
                Text = "Click me too!",
                Action = () => MessageBox.Show("You clicked me too!"),
                IsClosingAction = true
            });
    }
}