using System.Windows;

namespace Mdk.Notification.Windows;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    const int EmptyGracePeriod = 1000;

    async void OnIsEmptyChanged(object? sender, EventArgs e)
    {
        if (!Toast.IsEmpty)
            return;
        await Task.Delay(EmptyGracePeriod);
        if (Toast.IsEmpty)
            Shutdown();
    }

    void App_OnStartup(object sender, StartupEventArgs e)
    {
        Toast.IsEmptyChanged += OnIsEmptyChanged;

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