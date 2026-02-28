# SE Block G-Menu (Block Categories) Research

Sources:
- https://spaceengineers.wiki.gg/wiki/Modding/Tutorials/SBC/BlockCategories
- https://spaceengineers.wiki.gg/wiki/Modding/Reference/SBC/BlockCategories

## Summary

The G-menu block selector is driven entirely by `BlockCategories.sbc`, located at:

```
Content/Data/BlockCategories.sbc
```

Categories are additive — mods can append to existing categories or add their own by shipping a `BlockCategories.sbc` in their `Data/` folder.

---

## Category Definition Format

```xml
<Category xsi:type="MyObjectBuilder_GuiBlockCategoryDefinition">
  <Id>
    <TypeId>GuiBlockCategoryDefinition</TypeId>
    <SubtypeId/>
  </Id>
  <Name>Section1_Position2_Armorblocks</Name>
  <DisplayName>DisplayName_Category_ArmorBlocks</DisplayName>
  <StrictSearch>true</StrictSearch>
  <ItemIds>
    <string>TypeId/SubtypeId</string>
    <!-- ... -->
  </ItemIds>
</Category>
```

---

## Key Fields

| Field | Type | Default | Notes |
|---|---|---|---|
| `Name` | string | null | **Primary identifier** — SubtypeId is NOT used. Controls sort order. |
| `DisplayName` | string | — | Localization key or raw string |
| `ItemIds` | string[] | null | `TypeId/SubtypeId` pairs. Additive — same Name in two files merges lists. |
| `IsBlockCategory` | bool | **true** | For placeable blocks |
| `IsToolCategory` | bool | false | For hand-held or ship-mounted weapons/tools |
| `IsShipCategory` | bool | false | Show in ship toolbar (cockpit, RC, seat) |
| `IsAnimationCategory` | bool | false | For emotes |
| `StrictSearch` | bool | false | Prevents BlockVariantGroup expansion in this category |
| `SearchBlocks` | bool | true | Include in search results |
| `ShowInCreative` | bool | true | Visible in creative mode |

---

## Ordering

Categories are sorted **alphabetically by `Name`**. Vanilla uses structured names to control order:

```
Section1_Position1_Search
Section1_Position2_Armorblocks
Section1_Position2_Armorblocks_Fancy    ← mod-added, sorts after
Section1_Position3_Production
...
```

There is no explicit ordering field — position is entirely determined by lexicographic sort of `Name`.

---

## Sub-Categories

There are no actual sub-categories in the data model — the visual hierarchy in the G-menu is achieved by prefixing the `DisplayName` with **3 spaces**:

```xml
<DisplayName>   Fancy Armor Blocks</DisplayName>
```

The `Name` must still sort after its "parent" to appear visually grouped under it.

---

## Block Item IDs

Each entry in `ItemIds` is:
```
TypeId/SubtypeId
```

Examples:
- `CubeBlock/LargeBlockArmorBlock`
- `Assembler/LargeAssembler`
- `OxygenGenerator/`  ← empty subtype is valid

The `TypeId` here corresponds to the block definition type (NOT `MyObjectBuilder_` prefixed in this context). The `SubtypeId` is the block's subtype, e.g. `LargeBlockArmorBlock`.

A block can appear in **multiple categories** (and vanilla uses this extensively — `LargeBlocks` and `SmallBlocks` mirror all blocks from other categories).

---

## Implications for NodeScript Block Selector

To replicate the G-menu UI in the block selector:

1. **Parse `BlockCategories.sbc`** to get the category list and their `ItemIds`
2. **Sort categories by `Name`** to reconstruct the visual order
3. **Detect sub-categories** by checking if `DisplayName` starts with spaces
4. **For each block**, cross-reference with `CubeBlocks*.sbc` (or the block definitions) to get:
   - Display name
   - Icon/thumbnail (`.dds` textures in `Content/Textures/`)
   - Grid size (large/small)
5. Present the same two-panel layout SE uses: category list on left, block grid on right

### Files to Parse
- `Content/Data/BlockCategories.sbc` — category structure
- `Content/Data/CubeBlocks*.sbc` (multiple files) — block definitions, names, icons
- `Content/Textures/GUI/Icons/Cubes/` — block icon textures (.dds)
