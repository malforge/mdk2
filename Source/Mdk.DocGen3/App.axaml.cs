using System;
using System.Linq;
using Autofac;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Mdk.DocGen3.Features.Shell;
using MainWindow = Mdk.DocGen3.Features.Shell.MainWindow;

namespace Mdk.DocGen3;

public class App : Application
{
    IContainer? _container;

    public IContainer Container => _container ?? throw new InvalidOperationException("Container not initialized.");

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        InstallDependencyInjection();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            
            desktop.MainWindow = new ViewLocator().Build(this.Resolve<MainWindowViewModel>()) as MainWindow ?? throw new InvalidOperationException("MainWindow not found.");
        }

        base.OnFrameworkInitializationCompleted();
    }

    void InstallDependencyInjection()
    {
        var container = new ContainerBuilder();
        container.RegisterServices();
        container.RegisterViewAndModels();

        _container = container.Build();
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