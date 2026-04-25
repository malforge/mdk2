---
name: mdk-architecture
description: Use when working on MDK internals — the CLI tool (Mdk.CommandLine), packagers (Mdk.PbPackager / Mdk.ModPackager), analyzers (Mdk.PbAnalyzers / Mdk.ModAnalyzers), reference resolution (Mdk.References), or the script minification pipeline. Covers MSBuild integration flow (MdkRestore / MdkPack), CLI entry-point and project-type detection, analyzer integration with whitelists and AdditionalFiles, the 5-tier minification pipeline (none/trim/stripcomments/lite/full) and its processor stages, and how Space Engineers references are auto-detected. Trigger on edits to Source/Mdk.CommandLine/, Source/Mdk.PbAnalyzers/, Source/Mdk.ModAnalyzers/, Source/Mdk.References/, packager .props/.targets files, or questions about how mdk.exe processes scripts.
---

# MDK Architecture

## MSBuild Integration

User projects reference `Mal.Mdk2.PbPackager` or `Mal.Mdk2.ModPackager`. Their `.props` files hook the build:

- **Before build** — `MdkRestore`: runs `checkdotnet.exe`, then `mdk.exe restore <project>` to set up references and configuration.
- **After build** — `MdkPack`: runs only if `Configuration` matches `MdkBuildConfiguration` (default `all`); runs `mdk.exe pack <project>`. PB scripts are combined + minified into the Workshop folder; mods are copied into the game's Mods directory.

Set `<MdkBuildConfiguration>Release</MdkBuildConfiguration>` in a project to only pack on Release builds.

## CLI

**Entry**: `Source/Mdk.CommandLine/Program.cs` using a DI builder pattern (`Peripherals`):
- `IConsole` — logging abstraction with trace support
- `IInteraction` — user prompt abstraction
- `Parameters` — CLI parser with verb-specific sub-parameters

**Commands**:
- `mdk restore <project>` — initialize, detect Space Engineers, set up references
- `mdk pack <project>` — package script/mod based on project type

**Project type detection** (`MdkProject.LoadAsync()`) inspects `.csproj`/`.sln` and resolves to:
- `ProgrammableBlock` — PB script with `mdk.ini type=programmableblock`
- `Mod` — mod project with appropriate references
- `Legacy` — MDK1 project (auto-conversion supported)
- `Unknown` — not an MDK project

## Analyzers

Run at compile time in the IDE/build, **independent of the CLI tool**.
- `PbAnalyzers` validates against `pbwhitelist.dat`; `ModAnalyzers` against `modwhitelist.dat` (both embedded resources)
- Read INI files via `AdditionalFiles` (auto-included by packager props)
- Consume `Mdk-IgnorePaths` and `ProjectDir` MSBuild properties
- Ignore patterns implemented via `Microsoft.Extensions.FileSystemGlobbing`

**Custom MSBuild target `AddNuGetDlls`** (BeforeTargets="_GetPackageFiles") merges package references with compiled DLLs and packages analyzers + dependencies into `analyzers/dotnet/cs` (or `tools/netstandard2.0/cs` for MSBuild tasks). Uses `JoinItems` to combine `ResolvedCompileFileDefinitions` and `PackageReference`. Required for any NuGet package shipping custom MSBuild tasks or Roslyn analyzers with dependencies. Dependencies must be marked `PrivateAssets="all" IncludeInPackage="true"`.

## Minification Pipeline (PB only)

5 levels: `none` (combine only) → `trim` (remove unused types, not members) → `stripcomments` → `lite` (also trim whitespace) → `full` (also rename symbols).

Pipeline (`ScriptProcessingManager`):
```
Preprocessors → Combine → Postprocessors → Compose → PostComposition → Produce
```

Default processors:
- **Preprocessors** — symbol analysis, `#define` detection
- **Postprocessors** — TypeTrimmer, CommentStripper, CodeSmallifier, WhitespaceTrimmer, SymbolRenamer
- **Composer** — extracts `Program` class + helper types, flattens to a single script
- **PostComposition** — final validation, macro substitution
- **Producer** — writes output with `Instructions.readme` and `thumb.png`

Pipeline accepts custom processors implementing `IDocumentProcessor` interfaces.

## Namespaces (PB)

All PB script code lives in `IngameScript`. The game strips namespaces from the final script — avoid type name collisions across namespaces. Configure allowed namespaces in `mdk.ini`: `namespaces=IngameScript,MyHelpers`.

## Reference Resolution

`Mdk.References`:
- Auto-detects Space Engineers via registry, Steam library, or custom paths
- Generates MSBuild `.props`/`.targets` dynamically at restore time
- References game DLLs directly from the install (no redistribution)

For unit testing, set `<SpaceEngineersBinCopyLocal>true</SpaceEngineersBinCopyLocal>` to copy DLLs locally. **Never redistribute** — Keen Software House copyright.
