using System;
using Avalonia;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Interop;
using NuGet.Versioning;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using Velopack;

namespace Mdk.Hub;

sealed class Program
{
    // Flag set by OnFirstRun, checked by ShellViewModel
    internal static bool IsFirstRun { get; private set; }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        // Set up Velopack logging before bootstrap
        var logger = App.Container.Resolve<ILogger>();

        // Velopack bootstrap - MUST be first thing that runs
        VelopackApp.Build()
            .SetLogger(new VelopackLoggerAdapter(logger))
            .OnFirstRun(OnFirstRun)
            .Run();

        // Handle IPC before initializing Avalonia
        // Must stay synchronous with [STAThread] for COM/clipboard to work properly
        using (var ipc = new InterProcessCommunication.Standalone())
        {
            // If another instance is running, send message and exit
            if (ipc.IsAlreadyRunning())
            {
                if (args.Length > 0)
                    ipc.SendMessage(args); // Fully synchronous - no deadlock risk
                else
                    ipc.SendMessage(["--activate-hub"]);

                return 0; // Exit immediately
            }
        }

        // Start Avalonia UI
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    static void OnFirstRun(SemanticVersion version)
    {
        // This runs once after installation, before the UI starts
        IsFirstRun = true;
        var logger = App.Container.Resolve<ILogger>();
        logger.Info($"Velopack first run detected for version {version}");
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current
            .Register<FontAwesomeIconProvider>();

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
