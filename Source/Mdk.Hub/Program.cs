using Avalonia;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mdk.Hub.Features.Interop;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;

namespace Mdk.Hub;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task<int> Main(string[] args)
    {
        // Handle IPC before initializing Avalonia
        if (args.Length > 0)
        {
            using var ipc = new InterProcessCommunication.Standalone();
            
            // If another instance is running, send message and exit
            if (ipc.IsAlreadyRunning())
            {
                await ipc.SendMessageAsync(args);
                return 0; // Exit immediately
            }
            
            // We are first instance with args - they'll be handled via IPC MessageReceived
            // after services initialize, so just continue to start UI
        }
        
        // Start Avalonia UI
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
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