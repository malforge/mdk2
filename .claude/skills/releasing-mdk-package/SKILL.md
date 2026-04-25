---
name: releasing-mdk-package
description: Use when preparing a release for an MDK package — bumping PackageVersion.txt, writing user-facing release notes, or working with the CI/CD workflows that publish packages. Covers the bump-version workflow (PackageVersion.txt → ReleaseNotes.txt → CI), release notes style guidelines (user perspective, no technical jargon, focus on impact), and the buildwithartefacts.yml / version-guard.yml workflows. Trigger on edits to PackageVersion.txt or ReleaseNotes.txt, requests like "cut a release", "bump version", "add release notes", or working in .github/workflows/.
---

# Releasing an MDK Package

## Versioning workflow

When you modify code in a project that has a `PackageVersion.txt`, you must:

1. **Bump `PackageVersion.txt`** — single source of truth for the version number in that package directory. All packages use the same version per release. Read in MSBuild via `$([System.IO.File]::ReadAllText("$(MSBuildProjectDirectory)/PackageVersion.txt"))`.
2. **Add an entry to `ReleaseNotes.txt`** — describes the change to users (see guidelines below).
3. **CI/CD will trigger deployment** — `buildwithartefacts.yml` watches for changes and auto-publishes to NuGet.

## Release Notes Guidelines (User-Facing)

**For MDK users (Space Engineers script/mod developers) — communicates what changed between versions from their perspective.**

### Rules
- User perspective: describe capabilities, not implementation
- No technical jargon — avoid class names, method names, file paths
- Focus on impact: what can users now do? What works better?
- 1–2 sentences per change; group related changes into one bullet

### Examples
✅ "Minification now correctly handles unused fields"
✅ "Linux support: CLI and packagers work on Linux"
✅ "Build notifications now appear in MDK Hub"
❌ "Fixed NullReferenceException in TypeTrimmer.ProcessField()" (too technical)
❌ "Configuration drawer closes automatically after normalization" (UX flow, not user benefit)
❌ "Updated Mdk.References package to use new detection logic" (describes implementation)

## Release Notes location

Each package directory with `PackageVersion.txt` has a sibling `ReleaseNotes.txt`. Add new versions at the top: `v.X.Y.Z` followed by indented bullets. Update whenever you bump the version.

## CI/CD Workflows

- **`buildwithartefacts.yml`** — builds tools (win-x64) then packages; triggers on push to `main` when any `PackageVersion.txt` changes; auto-publishes to NuGet. Artifacts: `mdk.exe`, `mdknotify-win.exe`, `checkdotnet.exe`.
- **`version-guard.yml`** — PR validation: ensures `PackageVersion.txt` is updated when code changes, `ReleaseNotes.txt` includes the new version, and template package references are up to date.
