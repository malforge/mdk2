using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Interop;
using Mdk.Hub.Features.Projects;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Features.Snackbars;

namespace Mdk.Hub;

public class App : Application
{
    public static IDependencyContainer Container { get; } = new DependencyContainer();
    
    /// <summary>
    /// When true, simulates Linux behavior on Windows for testing purposes.
    /// </summary>
    public static bool SimulateLinux { get; private set; }
    
    /// <summary>
    /// Returns true if running on Linux or simulating Linux mode.
    /// </summary>
    public static bool IsLinux => SimulateLinux || OperatingSystem.IsLinux();

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var logger = Container.Resolve<ILogger>();
            logger.Info("MDK Hub application starting");

            // Set up global exception handlers
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var exception = e.ExceptionObject as Exception;
                logger.Error("Unhandled exception in AppDomain", exception ?? new Exception(e.ExceptionObject?.ToString() ?? "Unknown error"));
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                logger.Error("Unobserved task exception", e.Exception);
                e.SetObserved(); // Prevent process termination
            };

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

            // Initialize snackbar service with main window for screen detection
            var snackbarService = Container.Resolve<ISnackbarService>();
            if (snackbarService is SnackbarService ss)
                ss.SetMainWindow(shellWindow);

            var shell = Container.Resolve<IShell>();
            
            // Parse command line arguments
            var args = desktop.Args ?? Array.Empty<string>();
            SimulateLinux = args.Contains("--simulate-linux", StringComparer.OrdinalIgnoreCase);
            
            if (SimulateLinux)
                logger.Info("Running in Linux simulation mode");
            
            shell.Start(args);

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
