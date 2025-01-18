using Avalonia;
using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.DependencyInjection;
using JetBrains.Annotations;
using Mdk.Hub.Services;
using Mdk.Hub.ViewModels;
using Mdk.Hub.Views;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.MaterialDesign;

namespace Mdk.Hub;

[UsedImplicitly]
sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        IconProvider.Current
            .Register<MaterialDesignIconProvider>();

        var builder = new ContainerBuilder();
        RegisterServices(builder);
        RegisterViewModels(builder);
        
        Ioc.Default.ConfigureServices(new AutofacServiceProvider(builder.Build()));        
        
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    static void RegisterServices(ContainerBuilder builder)
    {
        builder.RegisterType<CommonDialogService>().As<ICommonDialogService>().SingleInstance();
    }

    static void RegisterViewModels(ContainerBuilder builder)
    {
        builder.RegisterType<MainWindowViewModel>().As<IApplication>().SingleInstance();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
