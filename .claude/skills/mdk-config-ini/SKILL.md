---
name: mdk-config-ini
description: Use when authoring or debugging mdk.ini / mdk.local.ini configuration files — covers the legacy/new naming convention and priority, scope (project vs developer-local), key options (type, minify levels, ignores, namespaces, trace, binarypath override), and standard ignore patterns. Trigger on creating or editing mdk.ini, mdk.local.ini, {ProjectName}.mdk.ini, when a user asks "how do I configure X in mdk.ini" or "what does <option> do", or when debugging why MDK isn't picking up settings.
---

# MDK INI Configuration

## Naming (priority order)

1. `mdk.ini` / `mdk.local.ini` (new)
2. `{ProjectName}.mdk.ini` / `{ProjectName}.mdk.local.ini` (legacy)

## Scope

- `mdk.ini` — project settings, checked in
- `mdk.local.ini` — developer overrides (e.g., custom binary paths), `.gitignore`d

## Key options

```ini
[mdk]
type=programmableblock                       # project type
minify=none|trim|stripcomments|lite|full     # minification level
ignores=obj/**/*,MDK/**/*,**/*.debug.cs      # exclusion globs
namespaces=IngameScript                      # allowed namespaces (comma-separated)
trace=on|off                                 # verbose output
binarypath=C:\Path\To\SpaceEngineers\Bin64   # override SE detection
```

## Standard ignore patterns

- `obj/**/*` — build output
- `MDK/**/*` — legacy MDK1 files
- `**/*.debug.cs` — debug-only helper files
