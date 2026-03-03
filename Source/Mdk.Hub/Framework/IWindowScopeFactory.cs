namespace Mdk.Hub.Framework;

/// <summary>
///     Creates window-scoped DI containers that provide window-specific services
///     while delegating all other resolutions to the application container.
/// </summary>
public interface IWindowScopeFactory
{
    /// <summary>
    ///     Creates a new <see cref="IWindowScope" /> for a window.
    /// </summary>
    IWindowScope Create();
}
