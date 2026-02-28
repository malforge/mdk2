using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.Shell;

/// <summary>
///     Manages overlay view models displayed within a single window.
///     Each window that hosts overlays should resolve its own instance.
/// </summary>
[Instance<IOverlayService>]
public class OverlayService : IOverlayService
{
    readonly ObservableCollection<object> _views = [];

    /// <inheritdoc />
    public ObservableCollection<object> Views => _views;

    /// <inheritdoc />
    public bool HasViews => _views.Count > 0;

    /// <inheritdoc />
    public void Show(OverlayModel overlay)
    {
        _views.Add(overlay);
        overlay.Dismissed += (_, _) =>
        {
            _views.Remove(overlay);
        };
    }

    /// <inheritdoc />
    public Task ShowAsync(OverlayModel overlay)
    {
        var tcs = new TaskCompletionSource();
        overlay.Dismissed += (_, _) => tcs.TrySetResult();
        Show(overlay);
        return tcs.Task;
    }
}
