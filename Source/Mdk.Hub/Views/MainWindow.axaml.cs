using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Mdk.Hub.ViewModels;

namespace Mdk.Hub.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    object? _oldDataContext;
    protected override void OnDataContextChanged(EventArgs e)
    {
        this.Close();
        base.OnDataContextChanged(e);
    }

    void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        var viewModel = DataContext as MainWindowViewModel;
        if (viewModel is null) return;

        if (!e.IsProgrammatic)
        {
            e.Cancel = true;
            try
            {
                viewModel.RequestCloseAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                // Dispatch the exception to the main thread
                Dispatcher.UIThread.Post(() => throw exception);
            }
        }
    }

    void OnMinifyButtonClick(object? sender, RoutedEventArgs e)
    {
        var viewModel = DataContext as MainWindowViewModel;
        WindowState = WindowState.Minimized;
        viewModel?.DidMinify();
    }

    async void OnCloseButtonClick(object? sender, RoutedEventArgs e)
    {
        var dlg = new MessageBoxWindow();
        await dlg.ShowDialog(this);
        
        var viewModel = DataContext as MainWindowViewModel;
        if (viewModel?.WillClose() ?? true)
        {
            Close();
            viewModel?.DidClose();
        }
    }
}