using System;
using System.Collections.Generic;
using System.ComponentModel;
using Autofac;
using Avalonia;
using Avalonia.Controls;
using Mdk.DocGen3.Features.ApiGenerator;
using Mdk.DocGen3.Features.Shell;
using IContainer = Autofac.IContainer;

namespace Mdk.DocGen3;

public static class ViewModelRegistration
{
    static readonly Dictionary<Type, Type> ViewModelMappings = new()
    {
        [typeof(MainWindowViewModel)] = typeof(MainWindow),
        [typeof(ApiGeneratorViewModel)] = typeof(ApiGeneratorView)
    };

    public static void RegisterViewAndModels(this ContainerBuilder container)
    {
        container.RegisterType<MainWindow>().AsSelf();
        container.RegisterType<MainWindowViewModel>().AsSelf();
        
        container.RegisterType<ApiGeneratorView>().AsSelf();
        container.RegisterType<ApiGeneratorViewModel>().AsSelf();
    }

    public static Control ViewForModel(this IContainer container, Type viewType)
    {
        if (ViewModelMappings.TryGetValue(viewType, out var viewModelType) && container.Resolve(viewModelType) is Control control)
            return control;

        throw new InvalidOperationException($"No ViewModel registered for {viewType}");
    }

    public static Control ViewForModel(this Application app, Type viewType)
    {
        if (app is App appInstance)
            return appInstance.Container.ViewForModel(viewType);

        throw new InvalidOperationException("Application instance is not of type App.");
    }
}