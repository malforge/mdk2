using System.Threading.Tasks;
using Mdk.Hub.Features.Shell;

namespace Mdk.Hub.Features.NodeScript.Selector;

/// <summary>
///     Extension methods for <see cref="IOverlayService" /> that provide typed selector results.
/// </summary>
public static class OverlayServiceExtensions
{
    /// <summary>
    ///     Shows the selector as an overlay and returns the selected item when dismissed,
    ///     or <c>null</c> if cancelled.
    /// </summary>
    public static async Task<TItem?> ShowSelectorAsync<TItem>(
        this IOverlayService service,
        SelectorViewModel<TItem> selector)
        where TItem : class
    {
        await service.ShowAsync(selector);
        return selector.SelectedItem;
    }
}
