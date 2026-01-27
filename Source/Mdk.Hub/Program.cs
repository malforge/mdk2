using Avalonia;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mdk.Hub.Features.Interop;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using Velopack;

namespace Mdk.Hub;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        // Velopack bootstrap - MUST be first thing that runs
        VelopackApp.Build()
            .OnFirstRun(OnFirstRun)
            .Run();
        
        // Handle IPC before initializing Avalonia
        // Must stay synchronous with [STAThread] for COM/clipboard to work properly
        if (args.Length > 0)
        {
            using var ipc = new InterProcessCommunication.Standalone();
            
            // If another instance is running, send message and exit
            if (ipc.IsAlreadyRunning())
            {
                ipc.SendMessage(args); // Fully synchronous - no deadlock risk
                return 0; // Exit immediately
            }
            
            // We are first instance with args - they'll be handled via IPC MessageReceived
            // after services initialize, so just continue to start UI
        }
        
        // Start Avalonia UI
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }
    
    static void OnFirstRun(NuGet.Versioning.SemanticVersion version)
    {
        // This runs once after installation, before the UI starts
        // Perfect place to check/install prerequisites
        
        // TODO: Check for .NET SDK
        // TODO: Install template package if needed
        
        // For now, just log that we ran
        Console.WriteLine($"MDK Hub {version} installed successfully!");
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