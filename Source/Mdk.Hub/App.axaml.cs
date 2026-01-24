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
using Mdk.Hub.Features.Shell;

namespace Mdk.Hub;

public class App : Application
{
    public static IDependencyContainer Container { get; } = new DependencyContainer();
    
    /// <summary>
    /// Startup arguments passed from Program.Main (if any).
    /// </summary>
    public static string[]? StartupArgs { get; set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var logger = Container.Resolve<ILogger>();
            logger.Info("MDK Hub application starting");
            
            var ipc = Container.Resolve<IInterProcessCommunication>();
            
            // Subscribe to IPC messages for debug logging
            ipc.MessageReceived += (_, e) =>
            {
                logger.Info($"IPC Message Received: Type={e.Message.Type}, Args={string.Join(", ", e.Message.Arguments)}");
            };
            
            // If we have startup args, handle them now
            if (StartupArgs is { Length: > 0 })
            {
                logger.Info($"Processing startup arguments: {string.Join(" ", StartupArgs)}");
                HandleCommandLineAsync(StartupArgs).ConfigureAwait(false);
            }
            
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            var shellViewModel = Container.Resolve<ShellViewModel>();
            var shellWindow = Container.Resolve<ShellWindow>();
            shellWindow.DataContext = shellViewModel;
            desktop.MainWindow = shellWindow;
            var shell = Container.Resolve<IShell>();
            shell.Start();
            
            logger.Info("MDK Hub application started successfully");
        }

        base.OnFrameworkInitializationCompleted();
    }

    async Task HandleCommandLineAsync(string[] args)
    {
        var ipc = Container.Resolve<IInterProcessCommunication>();
        var logger = Container.Resolve<ILogger>();
        
        logger.Info($"HandleCommandLineAsync called with {args.Length} args");
        
        try
        {
            // Parse command: <NotificationType> <arg1> <arg2> ...
            if (args.Length >= 1 && Enum.TryParse<NotificationType>(args[0], ignoreCase: true, out var type))
            {
                var messageArgs = args.Skip(1).ToArray();
                logger.Info($"Parsed message type: {type}, creating message with {messageArgs.Length} arguments");
                
                var message = new InterConnectMessage(type, messageArgs);
                logger.Info($"Calling ipc.SubmitAsync...");
                await ipc.SubmitAsync(message);
                logger.Info($"ipc.SubmitAsync completed - sent {type} notification with {messageArgs.Length} argument(s)");
            }
            else
            {
                logger.Warning($"Unknown command-line format: {string.Join(" ", args)}");
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error handling command-line arguments", ex);
        }
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