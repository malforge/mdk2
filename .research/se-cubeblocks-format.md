# SE CubeBlocks SBC & Localization Research

## Summary

Block definitions are split across many SBC files under:
```
Content/Data/CubeBlocks/
```

English display name strings are in:
```
Content/Data/Localization/MyTexts.resx   ← English (no locale suffix)
Content/Data/Localization/MyTexts.de.resx ← German, etc.
```

---

## CubeBlock Definition Format

Each block is a `<Definition>` element inside `<CubeBlocks>`:

```xml
<Definition>
  <Id>
    <TypeId>CubeBlock</TypeId>
    <SubtypeId>LargeBlockArmorBlock</SubtypeId>
  </Id>
  <DisplayName>DisplayName_Block_LightArmorBlock</DisplayName>
  <Icon>Textures\GUI\Icons\Cubes\light_armor_cube.dds</Icon>
  <Description>Description_LightArmor</Description>
  <CubeSize>Large</CubeSize>
  ...
</Definition>
```

### Key Fields

| Field | Notes |
|---|---|
| `Id/TypeId` | Short form already — **no** `MyObjectBuilder_` prefix (e.g. `CubeBlock`, `Refinery`) |
| `Id/SubtypeId` | Unique per type; empty string is valid |
| `DisplayName` | Localization key — must be resolved via RESX |
| `Icon` | Relative path from `Content/` — backslash separated, `.dds` file |
| `CubeSize` | `Large` or `Small` |

### TypeId Notes

- TypeId in SBC files is **already normalized** (no `MyObjectBuilder_` prefix)
- This matches exactly what `BlockCategories.sbc` uses in `ItemIds`
- No stripping needed during parsing
- Different block types use different `xsi:type` attributes on `<Definition>`, but we can ignore those — we only need `<Id>` children

### Icon Paths

Icon paths are relative to the `Content/` directory, using backslashes:
```
Textures\GUI\Icons\Cubes\light_armor_cube.dds
```
Full path on disk:
```
E:\Steam\steamapps\common\SpaceEngineers\Content\Textures\GUI\Icons\Cubes\light_armor_cube.dds
```

Avalonia cannot load `.dds` files natively — conversion or a DDS decoder library will be needed to display them.

---

## Localization (RESX)

Standard .NET RESX format. Each entry:
```xml
<data name="DisplayName_Block_LightArmorBlock" xml:space="preserve">
  <value>Light Armor Block</value>
</data>
```

- **English file**: `MyTexts.resx` (no locale suffix)
- Keys match the `DisplayName` values in SBC files exactly
- Parseable as XML — load as `XDocument`, query `root/data` elements by `name` attribute
- 9.4 MB file — should be loaded once and cached as a dictionary

### Example Lookups
| Key | Value |
|---|---|
| `DisplayName_Block_LightArmorBlock` | Light Armor Block |
| `DisplayName_Category_ArmorBlocks` | (need to query) |

---

## Data Pipeline

To build the block selector's data model:

1. **Load localization** — Parse `MyTexts.resx` into `Dictionary<string, string>` (key → English string)
2. **Load block definitions** — Parse all `Content/Data/CubeBlocks/*.sbc` files
   - For each `<Definition>`: extract `TypeId`, `SubtypeId`, `DisplayName` key, `Icon` path, `CubeSize`
   - Resolve `DisplayName` key against localization dictionary
   - Result: `Dictionary<(TypeId, SubtypeId), BlockInfo>`
3. **Load categories** — Parse `Content/Data/BlockCategories.sbc`
   - For each `<Category>`: extract `Name` (sort key), `DisplayName` key, `ItemIds`, `IsBlockCategory`
   - Resolve `DisplayName` key
   - Filter to `IsBlockCategory = true` only
   - Sort by `Name` alphabetically
   - Result: `List<BlockCategory>` ordered for display
4. **Join** — Each category's `ItemIds` → look up `BlockInfo` entries by `TypeId/SubtypeId`

---

## Minimal Data Model

```csharp
record BlockId(string TypeId, string SubtypeId);

record BlockInfo(
    BlockId Id,
    string DisplayName,
    string IconPath,   // relative to Content/
    string CubeSize    // "Large" | "Small"
);

record BlockCategory(
    string Name,        // sort key
    string DisplayName,
    bool IsSubCategory, // DisplayName starts with spaces in source
    List<BlockId> Items
);
```

---

## DDS Textures

Avalonia has no built-in DDS support. Options:
- **Pfim** (NuGet) — pure .NET DDS decoder, MIT license
- **ImageSharp** (NuGet) — broader image support, MIT license
- Load DDS → decode to RGBA byte array → create `WriteableBitmap` in Avalonia

This is a non-trivial step — plan as a separate task.

---

## Files to Parse (Minimal Set)

```
Content/Data/BlockCategories.sbc
Content/Data/CubeBlocks/*.sbc  (44 files currently)
Content/Data/Localization/MyTexts.resx
```
