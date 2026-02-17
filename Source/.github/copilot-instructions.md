# MDK2 - Malware's Development Kit for Space Engineers

MDK2 is a comprehensive toolkit for Space Engineers modding and scripting, consisting of Roslyn analyzers, MSBuild packagers, a command-line interface (CLI), and an Avalonia-based desktop application (Hub).

## Project Overview

This is a .NET 9 solution targeting Space Engineers mod and script development. The toolkit helps developers write, analyze, and package code that runs within Space Engineers' sandboxed environment.

### Core Components

- **Analyzers** (`Mdk.PbAnalyzers`, `Mdk.ModAnalyzers`): Roslyn analyzers that enforce Space Engineers whitelist restrictions
- **Packagers** (`Mdk.PbPackager`, `Mdk.ModPackager`): MSBuild tasks that compile multi-file projects into single scripts/mods
- **CLI** (`Mdk.CommandLine`): Command-line tool (`mdk.exe`) for packing scripts and mods
- **Hub** (`Mdk.Hub`): Avalonia desktop application providing GUI for project management
- **References** (`Mdk.References`): MSBuild task that auto-detects Space Engineers installation and references game assemblies
- **Extractor** (`Mdk.Extractor`): Utility that extracts whitelists and metadata from Space Engineers binaries
- **Templates** (`ScriptTemplates`): .NET templates for creating new scripts and mods
- **Source Generators** (`DISourceGenerator`, `LocSourceGenerator`): Code generation utilities

### Project Types

The system handles two main project types:

1. **Programmable Block Scripts** (PbScript): Single-file scripts for in-game programmable blocks
2. **Mods** (ModProject): Full game modifications

Both use multi-file projects during development but are packaged into formats the game can load.

## Build, Test, and Lint

### Building

```powershell
# Build entire solution
dotnet build MDK-Complete.slnx --configuration Release

# Build specific project
dotnet build Mdk.CommandLine\Mdk.CommandLine.csproj --configuration Release

# Build for specific platform (x64 required for Space Engineers integration)
dotnet build MDK-Complete.slnx --configuration Release /p:Platform=x64
```

### Testing

```powershell
# Run all tests
dotnet test Mdk.CommandLine.Tests\Mdk.CommandLine.Tests.csproj

# Run specific test
dotnet test Mdk.CommandLine.Tests\Mdk.CommandLine.Tests.csproj --filter "FullyQualifiedName~TestName"

# Run with verbosity
dotnet test Mdk.CommandLine.Tests\Mdk.CommandLine.Tests.csproj --logger "console;verbosity=detailed"
```

**Test Framework**: NUnit 4.3.2 with FakeItEasy for mocking

### Code Style

The repository uses `.editorconfig` for consistent formatting. Key conventions:

- **Naming**: camelCase for local functions, Unity serialized fields
- **var usage**: Prefer `var` for built-in types and when type is apparent
- **Line length**: Max 512 characters
- **Braces**: Required for multi-line control statements
- **Encoding**: UTF-8 with BOM, CRLF line endings

## Architecture

### Packaging Pipeline

Both PbPackager and ModPackager follow a similar processing pipeline:

1. **Load**: MSBuild workspace loads .csproj/.sln
2. **Filter**: Apply ignore patterns from `mdk.ini` (supports glob patterns)
3. **Process**: Chain of `IDocumentProcessor` implementations transform code:
   - Preprocessor conditionals
   - Comment stripping
   - Type trimming (removes unused types)
   - Symbol renaming (minification)
   - Namespace deletion (PbScript only)
   - Region annotations
4. **Combine/Compose**: Merge all files into single output
5. **Produce**: Generate final script/mod files

Key processor classes:
- `ScriptProcessingManager` / `ModProcessingManager`: Orchestrate the pipeline
- `ScriptPacker` / `ModPacker`: Entry points for packing operations
- Processors in `Pack/DefaultProcessors/`: Individual transformation steps

### Analyzer Architecture

Analyzers use Roslyn's `DiagnosticAnalyzer` to detect prohibited API usage:

- **Whitelist Loading**: Reads from embedded `pbwhitelist.dat` / `modwhitelist.dat` or `whitelist.cache` additional file
- **MDK-Ignorepaths**: Supports file/folder exclusion via glob patterns (from `mdk.ini` or MSBuild property)
- **Namespace Validation** (PbScript only): Warns if code isn't in configured namespaces

### Configuration System

Projects use `mdk.ini` files for configuration:

```ini
[mdk]
type=programmableblock|mod
trace=on|off
minify=none|trim|stripcomments|lite|full
minifyextraoptions=none|nomembertrimming
ignores=obj/**/*,MDK/**/*,**/*.debug.cs
namespaces=IngameScript  # PbScript only
```

For per-developer settings (e.g., custom Space Engineers path), use `<projectname>.mdk.local.ini` (should be in .gitignore).

### Dependency Injection (Hub Only)

The Hub uses a custom source generator (`DISourceGenerator`) for dependency injection:

- **`[Singleton]`** - Creates a single instance shared across the application (services, managers)
- **`[Instance]`** - Creates a new instance each time it's resolved (views, view models)
- **`[ViewModelFor<TView>]`** - Associates a ViewModel with its View for automatic pairing
- Generator produces `DependencyRegistry.g.cs` with registration code
- Access via `App.Container.Resolve<T>()` or `_container.Resolve<T>()`

**Critical**: When creating new Views (UserControls), **always** add `[Instance]` attribute:
```csharp
[Instance]
public partial class MyNewView : UserControl
```

ViewModels typically use:
```csharp
[Instance]  // or [Singleton] for shared state
[ViewModelFor<MyNewView>]
public class MyNewViewModel : ViewModel
```

The source generator scans the assembly and auto-generates registration code at compile time.

### Feature Organization (Hub)

The Hub follows a feature-based structure under `Mdk.Hub/Features/`:

- Each feature has its own folder (e.g., `Projects`, `Updates`, `Shell`)
- ViewModels extend `Model` base class (implements `INotifyPropertyChanged`)
- Commands use `RelayCommand` / `AsyncRelayCommand`
- Services use constructor injection

## Key Conventions

### File Naming

- **Platform-specific code**: `*.Windows.cs` and `*.Linux.cs` are conditionally compiled based on target framework
- **Dependent files**: Use `<DependentUpon>` in .csproj to group related files (e.g., `SymbolRenamer.SafeCharacters.cs` depends on `SymbolRenamer.cs`)
- **Debug files**: Files matching `**/*.debug.cs` are excluded from packing by default

### Roslyn/MSBuild Integration

- Analyzers and packagers are distributed as NuGet packages
- `PrivateAssets="all"` prevents transitive dependencies from leaking
- Packagers use `<IsRoslynComponent>false</IsRoslynComponent>` to avoid Roslyn version conflicts
- The References package uses custom MSBuild tasks to detect and reference Space Engineers binaries

### Script Length Limit

Programmable block scripts are limited to 100,000 characters (`ScriptPacker.MaxScriptLength`). Minification helps stay under this limit.

### Copyright Notice

All packages include the disclaimer from `CopyrightNotice.txt` stating no affiliation with Keen Software House.

## Common Tasks

### Adding a New Document Processor

1. Implement `IDocumentProcessor` in appropriate `DefaultProcessors` folder
2. Add to processor chain in `ScriptProcessingManager` or `ModProcessingManager`
3. Update `ProcessorFactories` if needed for custom configuration

### Updating Whitelists

1. Run `Mdk.Extractor` against Space Engineers binaries
2. Copy generated `pbwhitelist.dat` and `modwhitelist.dat` to analyzer projects
3. Set as embedded resource in .csproj

### Creating New Templates

1. Add template content under `ScriptTemplates/content/`
2. Use `UpdatePackageReferences.ps1` to sync NuGet versions before build
3. Template project structure: `<templatename>/<templatename>.csproj`

## Development Environment

- **IDE**: Visual Studio 2022 (recommended for stable Roslyn performance)
- **Runtime**: .NET 9.0
- **Platform**: Windows x64 (primary), Linux x64 (Hub only)
- **Space Engineers Required**: The References package requires Space Engineers installed to locate game assemblies

## CLI Usage

```powershell
# Pack a project
mdk pack <project.csproj|solution.sln>

# Pack with options
mdk pack <project> --minify full --trace --log output.log

# Restore (legacy conversion)
mdk restore <project>
```

The CLI supports both interactive mode (default) and non-interactive (`--non-interactive`) for CI/CD.

## Commit Messages and Release Notes

### Commit Message Guidelines

Commit messages should be concise but technical, describing **what changed** at a high level. They can mention implementation details when relevant but should stay focused and readable.

**Format:**
```
Brief summary of change

- Technical detail 1
- Technical detail 2
- Technical detail 3
```

**Good Examples:**
```
Improved notification snackbars

- Redesigned with horizontal layout (icon, message, buttons)
- Added configurable timeout in HubSettings with validation
- Implemented toast mode for message-only notifications
- Dynamic MaxWidth calculation at 50% screen width with text wrapping
```

```
Fixed Hub ignoring global custom output paths

- Updated CopyScriptToClipboardAsync to check HubSettings first
- Modified OpenOutputFolderAsync to resolve custom paths
- Added ISettings dependency to ProjectInfoAction
```

```
Refactored solution structure

- Migrated from .sln to .slnx format
- Removed obsolete Mdk.Notification project
- Updated GitHub Actions workflow for .slnx
```

**Bad Examples:**
```
Fixed stuff  // Too vague

Changed line 42 in ProjectService.cs to add null check  // Too specific

Implemented ISnackbarService.Show overload with isToast parameter and modified SnackbarViewModel to add IsToast property and updated SnackbarWindow.axaml to conditionally hide close button based on IsToast binding  // Too detailed
```

### Release Notes Guidelines

Release notes are **user-facing only**. Include only changes that users would notice or care about. Omit:
- Internal refactoring (unless it improves performance/stability noticeably)
- Dependency updates (unless they fix user-visible bugs)
- Code structure changes (.sln → .slnx migration, project removals, etc.)
- Build/CI changes

**Format:**
```
v.X.X.X-pre.XX
   - User-visible change 1
   - User-visible change 2
   - User-visible change 3
```

**Good Examples:**
```
v.1.0.0-pre.41
   - Added configurable deployment notification timeout in Global Settings > Advanced.
   - Improved snackbar notifications with horizontal layout and compact toast messages.
   - Fixed Hub not starting minimized when launched from build notifications.
   - Fixed "Open in Hub" button not restoring window from tray.
```

**What to Exclude:**
```
❌ Migrated solution files from .sln to .slnx format
❌ Removed obsolete Mdk.Notification project
❌ Updated Avalonia packages to 11.3.12
❌ Refactored ProjectService to use dependency injection
❌ Added unit tests for path resolution logic
```

**What to Include:**
```
✓ Added dark mode theme
✓ Fixed crash when opening project with missing files
✓ Improved snackbar appearance and usability
✓ Added option to customize notification timeout
✓ Fixed Hub ignoring custom output paths in settings
```

### Rule of Thumb

**Commit Message Test:** Would a developer reviewing the PR want to know this?
**Release Notes Test:** Would an end user notice or benefit from this?

## Publishing

Use `publish-prerelease.ps1` to merge current branch to prerelease branch and push (triggers automated builds).
