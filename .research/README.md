# Research

General research documents not tied to a specific project. These are findings, notes, and investigations that may inform multiple areas of development.

## Environment

- **Space Engineers installation**: `E:\Steam\steamapps\common\SpaceEngineers`
- **Block/game definitions (SBC files)**: `E:\Steam\steamapps\common\SpaceEngineers\Content\Data\`
- **Textures (DDS files)**: `E:\Steam\steamapps\common\SpaceEngineers\Content\Textures\`
- **Official modding wiki**: https://spaceengineers.wiki.gg/wiki/Modding/Tutorials
- **Modding API reference**: https://malforge.github.io/spaceengineers/modapi/
- **Programmable Block API reference**: https://malforge.github.io/spaceengineers/pbapi/

## Contents

- [se-block-categories.md](se-block-categories.md) — How the G-menu block selector is structured; `BlockCategories.sbc` format, ordering, sub-categories, block ID format, and implications for NodeScript block selector
- [se-cubeblocks-format.md](se-cubeblocks-format.md) — CubeBlock definition SBC format, icon paths, localization (RESX), minimal data model, and full data pipeline for the block selector
- [se-terminal-block-identification.md](se-terminal-block-identification.md) — How to identify terminal blocks via `MyTerminalInterfaceAttribute` + `MyCubeBlockTypeAttribute`; Cecil approach for static DLL analysis without running SE
