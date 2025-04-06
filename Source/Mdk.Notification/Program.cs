using System;
using Avalonia;
using JetBrains.Annotations;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.MaterialDesign;

namespace Mdk.Notification;

[UsedImplicitly]
sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        IconProvider.Current
            .Register<MaterialDesignIconProvider>();

        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .StartWithClassicDesktopLifetime(args);
    }
}