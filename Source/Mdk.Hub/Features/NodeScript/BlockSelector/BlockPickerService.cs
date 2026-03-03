using System.Threading.Tasks;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.NodeScript.Selector;
using Mdk.Hub.Features.Shell;

namespace Mdk.Hub.Features.NodeScript.BlockSelector;

/// <summary>
///     Resolves a fresh <see cref="BlockSelectorViewModel" /> via DI, shows it as an overlay,
///     and returns the selected block.
/// </summary>
[Singleton<IBlockPickerService>]
public class BlockPickerService : IBlockPickerService
{
    readonly IDependencyContainer _container;

    /// <summary>Initializes a new instance of <see cref="BlockPickerService" />.</summary>
    public BlockPickerService(IDependencyContainer container) => _container = container;

    /// <inheritdoc />
    public Task<BlockItem?> PickAsync(IOverlayService overlayService)
    {
        var vm = _container.Resolve<BlockSelectorViewModel>();
        return overlayService.ShowSelectorAsync(vm);
    }
}
