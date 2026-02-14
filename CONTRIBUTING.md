# Contributing to MDK²

Thanks for wanting to contribute to MDK²!

MDK² is used by many Space Engineers modders and scripters, but development is mostly just me with occasional help from a handful of contributors. This is a spare-time project, so pull requests might take a while to review. I focus on features I personally need, which means community contributions for other use cases are especially valuable. That said, I do reserve the right to reject contributions that don't fit the project's direction.

## What Should I Know?

### Project Overview

MDK² consists of several packages that work together:

Core packages (on NuGet): Mdk.CommandLine (the CLI tool), Mdk.PbPackager and Mdk.ModPackager (MSBuild integration), Mdk.PbAnalyzers and Mdk.ModAnalyzers (Roslyn analyzers), Mdk.References (finds your Space Engineers installation), and ScriptTemplates (project templates).

Support projects: Mdk.Hub (the GUI application) and Mdk.Extractor (pulls data from the game for whitelists).

### The Philosophy

From the README: "This is a project I pretty much made for myself. I'm publishing it in case someone else might have a use for it." Contributions are welcome, but they need to meet reasonable quality standards and align with what MDK² is trying to be.

## How to Contribute

### Reporting Bugs

Check the existing issues first to avoid duplicates. When filing a bug report, include:
- What you expected to happen
- What actually happened  
- Steps to reproduce it
- Your environment (OS, .NET version, Space Engineers version, MDK version)
- Any error messages or logs

### Suggesting Features

File an issue explaining what you'd like to see and why it would be useful. Keep in mind that features outside my personal use cases will likely need someone to implement them.

### Pull Requests

Fork the repo, make your changes, and submit a PR against `main`. Make sure tests pass and follow the versioning rules below. If you're changing functionality, update the docs too.

## The Important Part: Versioning

This one's critical. When you modify code in any package, you **must** update both the version and release notes.

Each package has a `PackageVersion.txt` and a `ReleaseNotes.txt` file. If you change the code, bump the version (patch for bugs, minor for features, major for breaking changes) and add release notes.

### Release Notes Are User-Facing

Release notes are written for people using MDK to write scripts, not for people working on MDK itself. Describe what changed from their perspective. Don't use class names, method names, or talk about implementation details.

Good examples:
- "Minification now correctly handles unused fields"
- "Linux support: CLI and packagers work on Linux"
- "Fixed crash when packing projects with shared imports"

Bad examples:
- "Fixed NullReferenceException in TypeTrimmer.ProcessField()"
- "Updated Mdk.References to use new detection logic"  
- "Configuration drawer closes automatically after normalization"

Format is simple - new version at the top with bullet points below:

```
v.2.1.6
   - Brief description of what changed for users
   - Another improvement

v.2.1.5
   - Previous version
```

### Commit Messages Are Developer-Facing

Commit messages can be technical. They're for people working on MDK, so feel free to mention class names or specific changes when it helps clarify what you did.

Just keep them reasonably clear and specific. "Fix TypeTrimmer crash on unused fields" is way more helpful than "Fix bug". Reference issues if relevant.

Examples of helpful commits:
- "Add --trace flag support to CLI parameters"
- "Fix TypeTrimmer crash on unused fields with initializers"
- "Update version validation to check branch naming"

Less helpful:
- "Fix bug"
- "Update code"
- "Various improvements"

### Branch Conventions

When contributing, you'll work on your own branch and submit a pull request to merge into `main`.

Branch names can be whatever makes sense to you - something like `fix-analyzer-crash` or `add-linux-support` or just your username like `john-doe-fixes` all work fine.

**About main and prerelease branches:**
- `main` - where stable releases happen. Versions look like `2.1.5` (no suffix)
- `prerelease` - where I do testing before stable releases. Versions look like `2.2.0-pre.3` (with a suffix)

You'll almost always be targeting `main` with your pull requests. The CI/CD pipeline will check that you updated versions correctly.

## Development Setup

You'll need .NET 9.0 SDK, an IDE (Visual Studio 2022 works well), and Space Engineers installed.

Clone the repo and build it:
```bash
cd Source
dotnet build MDK-Complete.sln
```

Run tests with:
```bash
dotnet test MDK-Complete.sln
```

We use NUnit for tests.

For debugging help, see [.github/debugging-mdk.md](.github/debugging-mdk.md).

## Questions?

Check the [documentation](https://malforge.github.io/spaceengineers/mdk2/) or file an issue.

---

Thanks for helping make MDK² better!


