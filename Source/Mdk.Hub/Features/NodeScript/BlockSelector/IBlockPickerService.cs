using System.Threading.Tasks;
using Mdk.Hub.Features.Shell;

namespace Mdk.Hub.Features.NodeScript.BlockSelector;

/// <summary>
///     Presents the block type picker overlay and returns the chosen block.
/// </summary>
public interface IBlockPickerService
{
    /// <summary>
    ///     Shows the block picker as an overlay via <paramref name="overlayService" /> and returns the chosen
    ///     <see cref="BlockItem" />, or <c>null</c> if the user cancelled.
    /// </summary>
    Task<BlockItem?> PickAsync(IOverlayService overlayService);
}
