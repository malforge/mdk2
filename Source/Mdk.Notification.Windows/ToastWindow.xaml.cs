using System.Windows;

namespace Mdk.Notification.Windows;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class ToastWindow : Window
{
    /// <summary>
    ///     Creates a new instance of the <see cref="ToastWindow" /> class.
    /// </summary>
    public ToastWindow()
    {
        InitializeComponent();
    }

    void CloseButton_OnClick(object sender, RoutedEventArgs e) => Close();

    void Hyperlink_OnClick(object sender, RoutedEventArgs e)
    {
        var hyperlink = (Hyperlink)sender;
        var action = (ToastAction)hyperlink.DataContext;
        if (action.IsClosingAction)
            Close();
        action.Action();
    }
}