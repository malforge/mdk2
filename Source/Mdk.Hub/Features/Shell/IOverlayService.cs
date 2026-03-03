using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Mdk.Hub.Features.Shell;

/// <summary>
///     Manages overlay view models displayed within a window.
/// </summary>
public interface IOverlayService
{
    /// <summary>Gets the collection of overlay view models currently displayed.</summary>
    ObservableCollection<object> Views { get; }

    /// <summary>Gets whether any overlays are currently displayed.</summary>
    bool HasViews { get; }

    /// <summary>Adds and shows the given overlay. Automatically removes it when dismissed.</summary>
    void Show(OverlayModel overlay);

    /// <summary>
    ///     Adds and shows the given overlay, returning a task that completes when it is dismissed.
    /// </summary>
    Task ShowAsync(OverlayModel overlay);
}
