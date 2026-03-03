namespace Mdk.Hub.Framework;

/// <summary>
///     Implemented by view models that provide a title for their host window.
/// </summary>
public interface IHaveATitle
{
    /// <summary>Gets the title to display on the host window.</summary>
    string Title { get; }
}
