using System.ComponentModel;

namespace Mdk.Hub.Framework;

/// <summary>
///     Interface for view models that provide a dynamic window title.
/// </summary>
public interface IHaveATitle : INotifyPropertyChanged
{
    /// <summary>
    ///     Gets the window title.
    /// </summary>
    string Title { get; }
}
