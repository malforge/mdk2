using System;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Framework;

[Singleton<IWindowScopeFactory>]
class WindowScopeFactory : IWindowScopeFactory
{
    readonly IDependencyContainer _appContainer;

    public WindowScopeFactory(IDependencyContainer appContainer)
    {
        _appContainer = appContainer;
    }

    public IWindowScope Create()
    {
        var container = new DependencyContainerBuilder()
            .AddRegistry<WindowGeneratedRegistry>()
            .UseFallback((IServiceProvider)_appContainer)
            .Build();
        return new WindowScope(container);
    }
}
