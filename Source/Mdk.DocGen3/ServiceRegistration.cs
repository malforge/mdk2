using System;
using Autofac;
using Avalonia;

namespace Mdk.DocGen3;

public static class ServiceRegistration
{
    public static void RegisterServices(this ContainerBuilder container) { }

    public static T Resolve<T>(this Application app) where T : class
    {
        if (app is App appInstance)
            return appInstance.Container.Resolve<T>();

        throw new InvalidOperationException("Application instance is not of type App.");
    }
}