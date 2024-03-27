using System.Windows;

namespace Mdk.Notification.Windows;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class ToastWindow : Window
{
    Task _waitTask = Task.CompletedTask;

    /// <summary>
    ///     Creates a new instance of the <see cref="ToastWindow" /> class.
    /// </summary>
    public ToastWindow()
    {
        InitializeComponent();
    }

    void CloseButton_OnClick(object sender, RoutedEventArgs e) => Close();

    async void Hyperlink_OnClick(object sender, RoutedEventArgs e)
    {
        var hyperlink = (Hyperlink)sender;
        var action = (ToastAction)hyperlink.DataContext;
        var args = new ToastActionArguments();
        action.Action(args);
        _waitTask = args.HoldTask;
        if (action.OneTimeOnly)
            hyperlink.IsEnabled = false;
        if (action.IsClosingAction)
            await CloseAsync();
    }

    public async Task CloseAsync()
    {
        await _waitTask;
        Close();
    }
}