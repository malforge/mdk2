using System.Collections.Generic;

namespace Mdk.Hub.Features.SpaceEngineers;

/// <summary>
///     A block category as shown in the Space Engineers G-menu (Toolbar Config).
///     Categories are ordered by <see cref="Name" /> alphabetically.
/// </summary>
/// <param name="Name">
///     The sort key and unique identifier (e.g., <c>Section1_Position2_Armorblocks</c>).
///     This is NOT the display name.
/// </param>
/// <param name="DisplayName">The resolved English display name, with leading spaces stripped.</param>
/// <param name="IsSubCategory">
///     <c>true</c> if the original display name had leading spaces, indicating visual sub-category indentation.
/// </param>
/// <param name="Items">The block IDs listed in this category, in SBC file order.</param>
public sealed record BlockCategory(string Name, string DisplayName, bool IsSubCategory, IReadOnlyList<BlockId> Items);
