using System.Diagnostics;
using Avalonia.Controls;

namespace Mdk.Notification.Views;

public partial class ToastWindow : Window
{
    public ToastWindow()
    {
        InitializeComponent();
    }

    void WindowBase_OnResized(object? sender, WindowResizedEventArgs e)
    {
        Debug.WriteLine(e.ClientSize + " " + e.Reason);
    }
}