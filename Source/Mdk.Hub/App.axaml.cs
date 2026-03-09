using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
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

/// <summary>
///     The main application class for MDK Hub, responsible for initialization and lifetime management.
/// </summary>
public class App : Application
{
    static readonly IDependencyContainer _container = CreateContainer();

    /// <summary>
    ///     When true, simulates Linux behavior on Windows for testing purposes.
    /// </summary>
    public static bool SimulateLinux { get; private set; }

    /// <summary>
    ///     Returns true if running on Linux or simulating Linux mode.
    /// </summary>
    public static bool IsLinux => SimulateLinux || OperatingSystem.IsLinux();

    internal static ILogger GetLogger() => _container.Resolve<ILogger>();

    static IDependencyContainer CreateContainer()
    {
        IDependencyContainer? container = null;
        container = new DependencyContainerBuilder()
            .AddRegistry<GeneratedRegistry>()
            .RegisterSingleton<IDependencyContainer>(() => container!)
            .Build();
        return container;
    }

    /// <summary>
    ///     Initializes the application by loading XAML resources.
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        DataTemplates.Add(new ViewLocator(_container));
    }

    /// <summary>
    ///     Called when the framework initialization is completed, sets up services, exception handlers, and the main window.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var logger = _container.Resolve<ILogger>();
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
            _container.Resolve<IInterProcessCommunication>();
            _container.Resolve<IProjectService>();

            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            EasterEggBehavior.Service = new Lazy<IEasterEggService>(_container.Resolve<IEasterEggService>);
            var shellViewModel = _container.Resolve<ShellViewModel>();
            var shellWindow = _container.Resolve<ShellWindow>();

            // Parse command line arguments
            var args = desktop.Args ?? Array.Empty<string>();
            SimulateLinux = args.Contains("--simulate-linux", StringComparer.OrdinalIgnoreCase);

            if (SimulateLinux)
                logger.Info("Running in Linux simulation mode");

            // Set window to minimized BEFORE setting DataContext if launching with notification arguments
            // This prevents the window from flashing visible before being minimized
            if (NotificationCommand.IsNotificationCommand(args))
                shellWindow.WindowState = WindowState.Minimized;

            shellWindow.DataContext = shellViewModel;

            desktop.MainWindow = shellWindow;

            // Initialize snackbar service with main window for screen detection
            var snackbarService = _container.Resolve<ISnackbarService>();
            if (snackbarService is SnackbarService ss)
                ss.SetMainWindow(shellWindow);

            var shell = _container.Resolve<IShell>();
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