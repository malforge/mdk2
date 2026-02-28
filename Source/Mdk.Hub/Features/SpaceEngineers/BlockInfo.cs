namespace Mdk.Hub.Features.SpaceEngineers;

/// <summary>
///     Metadata for a Space Engineers block definition, sourced from SBC files.
/// </summary>
/// <param name="Id">The block's type and subtype identifiers.</param>
/// <param name="DisplayName">The resolved English display name.</param>
/// <param name="IconPath">
///     Icon path relative to the <c>Content/</c> directory (e.g., <c>Textures\GUI\Icons\Cubes\light_armor_cube.dds</c>),
///     or <c>null</c> if the block has no icon.
/// </param>
/// <param name="Dlc">The DLC identifier required to use this block, or <c>null</c> if the block is base-game.</param>
/// <param name="CubeSize">Grid size: <c>"Large"</c> or <c>"Small"</c>.</param>
public sealed record BlockInfo(BlockId Id, string DisplayName, string? IconPath, string CubeSize, string? Dlc = null);
