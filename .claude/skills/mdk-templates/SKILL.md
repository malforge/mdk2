---
name: mdk-templates
description: Use when working in Source/ScriptTemplates/content/ or modifying the .NET project templates that MDK ships for `dotnet new`. Covers the standard template directory layout (.template.config, mdk.ini, Instructions.readme, thumb.png, Program.cs) and the IngameScript namespace requirement for PB script templates. Trigger on edits to Source/ScriptTemplates/, creating a new template, or questions about how MDK templates are structured.
---

# MDK Project Templates

Templates live in `Source/ScriptTemplates/content/`:
- `0_Script` — PB script template
- `1_Mod` — mod template
- `2_Mixin` — shared project template

## Standard template layout

- `.template.config/` — VS / `dotnet new` template metadata (`template.json`, etc.)
- `mdk.ini` — project config (sets `type`, minification level, ignores, etc.)
- `Instructions.readme` — injected as a header comment in the packed output
- `thumb.png` — 512×512 Workshop thumbnail
- `Program.cs` — entry point. **For PB scripts, must be in the `IngameScript` namespace.**

## Notes

- Templates may not be fully configured for direct testing — use `Source/Mdk.CommandLine.Tests/TestData/` projects when you need a known-good test target.
- When updating template package references, the `version-guard.yml` workflow validates they're up to date on PRs.
