using System;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Framework;

/// <summary>
///     Represents the DI scope for a single window. Disposing this scope releases window-scoped services.
/// </summary>
public interface IWindowScope : IDisposable
{
    /// <summary>
    ///     Gets the dependency container for this window scope.
    /// </summary>
    IDependencyContainer Container { get; }
}
