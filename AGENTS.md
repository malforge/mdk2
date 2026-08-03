⚠️ **Branch safety**: Check the current git branch at the start of every session. If on `main` or `prerelease`, warn the user and suggest creating/switching to a feature branch — these are protected and should not receive direct commits during development.

## Commit Message Guidelines (Developer-Focused)

**For developers tracking technical changes in the codebase.**

### Rules
- **Check uncommitted changes first** — always review `git status` and `git diff` to see what's actually changed
- **Describe only uncommitted changes — do NOT duplicate previous commits.** A common past failure: re-listing things from prior commits in the next commit message. The new commit message describes only what's in the *current* uncommitted diff.
- Subject line under 72 chars; body 2–3 sentences max
- Focus on **what and why**, not how — the diff shows implementation
- Be specific: "Fix TypeTrimmer null reference when processing empty classes" beats "Fix bug"
- Don't enumerate files/methods — describe the change conceptually
- Cover all relevant uncommitted changes, not just the most recent work

### Examples
✅ "Add --trace flag support to CLI parameters"
✅ "Fix TypeTrimmer crash on unused fields with initializers"
✅ "Update buildwithartefacts.yml to validate version suffixes per branch"
❌ "Fix bug" (too vague)
❌ "Update code" (what code?)
❌ "Made some changes to the packager" (what changes?)

## Pull Request Descriptions (Reviewer-Focused)

**Just the facts of what is added or changed.**

### Rules
- Open with one short paragraph of what and why, written for a reviewer who was not part of any
  discussion around the change and has no context beyond the diff
- Then bulleted lists grouped by area, one line per change
- Verification results get one line, not a table
- 25 lines or fewer
- Call out anything untested, or that needs follow-up elsewhere
- No narrative of how the work went, no justification of individual design choices, no restating the diff

### Examples
✅ "Extractor now runs the Space Engineers dedicated server instead of launching the retail game in-process, which makes it runnable in CI."
✅ "Blocks keyed by `typedefinition` instead of a single interface; `type` retained so DocGen keeps working"
✅ "Verified against previous output: whitelists identical, 0 blocks lost, 12 recovered"
❌ "Two behaviours had to change: ..." (narrative)
❌ "I kept X committed because otherwise Y would happen" (reasoning belongs in the conversation)

# MDK2 Development Guide

MDK² (Malware's Development Kit for Space Engineers) is a toolkit for developing programmable block scripts and mods for Space Engineers — NuGet packages, Roslyn analyzers, a CLI tool, and MSBuild integration.

## Quick Reference

### Building
```bash
dotnet build Source\MDK-Complete.slnx                           # all projects + tests + generators
dotnet build Source\MDK-Packages.slnx                           # packages only (no tests/generators/Hub)
dotnet build Source\Mdk.CommandLine\Mdk.CommandLine.csproj      # one project
dotnet build Source\MDK-Packages.slnx -c Release                # release packages (auto-generates NuGet packages)
```

### Testing (NUnit + NUnit3TestAdapter)
```bash
dotnet test Source\MDK-Complete.slnx
dotnet test Source\Mdk.CommandLine.Tests\Mdk.CommandLine.Tests.csproj
dotnet test --filter "FullyQualifiedName~TestMethodName"
dotnet test --filter "FullyQualifiedName~ClassName"
```

### Linting
None configured — relies on built-in Roslyn analyzers and IDE warnings.

## Project Structure

**Core packages (NuGet)**
- `Mdk.CommandLine` — main CLI tool (`mdk.exe`) with restore/pack commands
- `Mdk.PbPackager` / `Mdk.ModPackager` — MSBuild integration for PB scripts / mods
- `Mdk.PbAnalyzers` / `Mdk.ModAnalyzers` — Roslyn analyzers for whitelist validation (netstandard2.0)
- `Mdk.References` — auto-detects Space Engineers install, sets up assembly references
- `ScriptTemplates` — .NET templates for new projects

**Support**
- `Mdk.Extractor` — extracts game data (whitelists, script prologue, terminal actions) by running a headless Space Engineers dedicated server

**Hub**
- `Mdk.Hub` — GUI management app (Avalonia UI, Windows and Linux)

## Development Environment

- **Target framework**: .NET 9.0 (CLI + tests), netstandard2.0 (analyzers)
- **IDE**: any C# environment — Visual Studio, Rider, VS Code
- **Platform**: Windows and Linux. The game itself is Windows-only, but the tooling runs on both; the Hub ships a Linux AppImage and the CLI and packagers work there. Building the full solution needs Windows because Mdk.Hub also targets `net9.0-windows`.
- **Roslyn**: Microsoft.CodeAnalysis.CSharp.Workspaces 4.12.0

## Specialized topics — see skills

These skills auto-load when their topic comes up — you don't need to invoke them manually:

- **`debugging-mdk`** — reproducing/debugging MDK CLI bugs, NuGet-vs-source comparison, debugger setup, capturing trace output
- **`mdk-architecture`** — MSBuild integration (MdkRestore/MdkPack), CLI internals, analyzers, minification pipeline, reference resolution
- **`mdk-config-ini`** — authoring `mdk.ini` / `mdk.local.ini` configuration files
- **`releasing-mdk-package`** — bumping `PackageVersion.txt`, writing user-facing release notes, CI/CD workflows
- **`mdk-templates`** — working on `.NET` project templates in `Source/ScriptTemplates/`
