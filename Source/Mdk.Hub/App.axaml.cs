using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Interop;
using Mdk.Hub.Features.Projects;
using Mdk.Hub.Features.Shell;

namespace Mdk.Hub;

public class App : Application
{
    public static IDependencyContainer Container { get; } = new DependencyContainer();

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var logger = Container.Resolve<ILogger>();
            logger.Info("MDK Hub application starting");
            
            // Initialize services (ProjectService subscribes to IPC internally)
            Container.Resolve<IInterProcessCommunication>();
            Container.Resolve<IProjectService>();
            
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            var shellViewModel = Container.Resolve<ShellViewModel>();
            var shellWindow = Container.Resolve<ShellWindow>();
            shellWindow.DataContext = shellViewModel;
            desktop.MainWindow = shellWindow;
            var shell = Container.Resolve<IShell>();
            shell.Start(desktop.Args ?? Array.Empty<string>());
            
            logger.Info("MDK Hub application started successfully");
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
            BindingPlugins.DataValidators.Remove(plugin);
    }
}