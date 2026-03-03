using System;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Framework;

sealed class WindowScope : IWindowScope
{
    public WindowScope(IDependencyContainer container)
    {
        Container = container;
    }

    public IDependencyContainer Container { get; }

    public void Dispose()
    {
        if (Container is IDisposable disposable)
            disposable.Dispose();
    }
}
