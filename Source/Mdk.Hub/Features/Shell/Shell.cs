using Mal.DependencyInjection;

namespace Mdk.Hub.Features.Shell;

[Dependency<IShell>]
public class Shell(IDependencyContainer container) : IShell
{
    readonly IDependencyContainer _container = container;

    public void Start() { }
}