using System.Collections.Generic;
using System.Threading.Tasks;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript.BlockDefinitions;

/// <summary>
///     Provides Space Engineers block definition data parsed from SBC files.
///     Data is loaded lazily on first request and cached for subsequent calls.
/// </summary>
public interface IBlockDefinitionService
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
