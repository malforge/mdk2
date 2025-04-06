using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Mdk.Notification.Services;
using Mdk.Notification.ViewModels;

namespace Mdk.Notification;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            BindingPlugins.DataValidators.RemoveAt(0);
            desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
            var toasts = new Toasts();

            var toast = new MessageToast("Hello, World!", [new ToastAction("OK", t => { t?.Dismiss(); })]);
            toasts.ShowToast(toast);
            //await Task.Delay(1000);
            toast = new MessageToast("This is a test toast.", [new ToastAction("OK", t => { t?.Dismiss(); })]);
            toasts.ShowToast(toast);
            //await Task.Delay(1000);
            toast = new MessageToast("This is a test toast.", [new ToastAction("OK", t => { t?.Dismiss(); })]);
            toasts.ShowToast(toast);
            //await Task.Delay(1000);
            toast = new MessageToast("This is a test toast.", [new ToastAction("OK", t => { t?.Dismiss(); })]);
            toasts.ShowToast(toast);
            //await Task.Delay(1000);
            toast = new MessageToast("This is a test toast.", [new ToastAction("OK", t => { t?.Dismiss(); })]);
            toasts.ShowToast(toast);
        }

        base.OnFrameworkInitializationCompleted();
    }
}