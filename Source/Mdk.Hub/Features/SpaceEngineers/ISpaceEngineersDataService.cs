using System.Collections.Generic;
using System.Threading.Tasks;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.SpaceEngineers;

/// <summary>
///     Provides Space Engineers block definition data loaded from the game's SBC files.
///     Data is loaded on first request, cached on disk, and the cache is automatically
///     refreshed when source files change.
/// </summary>
public interface ISpaceEngineersDataService
{
    /// <summary>
    ///     Gets all block categories ordered as they appear in the Space Engineers G-menu.
    /// </summary>
    Task<ApiResult<IReadOnlyList<BlockCategory>>> GetCategoriesAsync();

    /// <summary>
    ///     Gets metadata for a specific block by its type and subtype IDs.
    /// </summary>
    /// <param name="typeId">The block TypeId without the <c>MyObjectBuilder_</c> prefix.</param>
    /// <param name="subtypeId">The block SubtypeId. May be an empty string.</param>
    Task<ApiResult<BlockInfo>> GetBlockAsync(string typeId, string subtypeId);
}
