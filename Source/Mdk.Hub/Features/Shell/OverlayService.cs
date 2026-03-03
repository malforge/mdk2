using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.Shell;

/// <summary>
///     Manages overlay view models displayed within a single window.
///     Each window that hosts overlays should resolve its own instance.
/// </summary>
[Singleton<IOverlayService>(Container = "Window")]
public class OverlayService : IOverlayService
{
    /// <inheritdoc />
    public ObservableCollection<object> Views { get; } = [];

    /// <inheritdoc />
    public bool HasViews => Views.Count > 0;

    /// <inheritdoc />
    public void Show(OverlayModel overlay)
    {
        Views.Add(overlay);
        overlay.Dismissed += (_, _) => { Views.Remove(overlay); };
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
