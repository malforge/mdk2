# MDK2 Development Guide

MDK² (Malware's Development Kit for Space Engineers) is a toolkit for developing programmable block scripts and mods for Space Engineers. It consists of NuGet packages, Roslyn analyzers, a CLI tool, and MSBuild integration.

## Quick Reference

### Building
```bash
# Build the complete solution (all projects including tests and generators)
dotnet build Source\MDK-Complete.sln

# Build packages only (excludes tests, generators, and Hub)
dotnet build Source\MDK-Packages.sln

# Build a specific project
dotnet build Source\Mdk.CommandLine\Mdk.CommandLine.csproj

# Create release packages (auto-generates NuGet packages for analyzer projects)
dotnet build Source\MDK-Packages.sln -c Release
```

### Testing
```bash
# Run all tests
dotnet test Source\MDK-Complete.sln

# Run tests for a specific project
dotnet test Source\Mdk.CommandLine.Tests\Mdk.CommandLine.Tests.csproj

# Run specific test by name
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Run tests in a specific test class
dotnet test --filter "FullyQualifiedName~ClassName"
```

**Test Framework**: NUnit 4.3.2 with NUnit3TestAdapter 4.6.0

### Linting
No specific linters configured. Code analysis relies on built-in Roslyn analyzers and IDE warnings.

## Project Structure

### Core Packages (distributed via NuGet)
- **Mdk.CommandLine** - Main CLI tool (`mdk.exe`) with restore/pack commands
- **Mdk.PbPackager** - MSBuild integration for Programmable Block projects
- **Mdk.ModPackager** - MSBuild integration for Mod projects
- **Mdk.PbAnalyzers** - Roslyn analyzers for PB script validation (netstandard2.0)
- **Mdk.ModAnalyzers** - Roslyn analyzers for Mod validation (netstandard2.0)
- **Mdk.References** - Auto-detects Space Engineers installation and sets up assembly references
- **ScriptTemplates** - .NET templates for creating new projects

### Support Projects
- **Mdk.CheckDotNet** - .NET SDK availability checker
- **Mdk.Notification.Windows** - Toast notifications for build completion
- **Mdk.Extractor** - Extracts game data for whitelists

### Hub and Document Generators
- **Mdk.Hub** - GUI management application (Avalonia UI)
- **Mdk.DocGen3** - Primary documentation generator
- **Mdk.DocGen2.*** - Legacy documentation tools (ApiDocs, Sprites, Terminals, TypeDef)

## Architecture

### MSBuild Integration Flow

User projects reference `Mal.Mdk2.PbPackager` or `Mal.Mdk2.ModPackager` NuGet packages. These packages include `.props` files that hook into the build process:

**Before Build**: `MdkRestore` target
1. Runs `checkdotnet.exe` to verify .NET SDK
2. Runs `mdk.exe restore <project>` to set up references and configuration

**After Build**: `MdkPack` target
1. Only runs if `Configuration` matches `MdkBuildConfiguration` property (default: `all`)
2. Runs `mdk.exe pack <project>` to package the script or mod
3. For PB scripts: combines files, applies minification, outputs to Workshop folder
4. For Mods: copies files to game's Mods directory

**Disable auto-pack**: Set `<MdkBuildConfiguration>Release</MdkBuildConfiguration>` in project to only pack on Release builds.

### CLI Architecture

**Entry Point**: `Program.cs` using dependency injection builder pattern (`Peripherals`)
- `IConsole` - Logging abstraction with trace support
- `IInteraction` - User prompt abstraction
- `Parameters` - Command-line parser with verb-specific sub-parameters

**Commands**: 
- `mdk restore <project>` - Initialize MDK project, detect Space Engineers, set up references
- `mdk pack <project>` - Package script/mod based on project type

**Project Type Detection**: `MdkProject.LoadAsync()` examines `.csproj` or `.sln` files to determine:
- `ProgrammableBlock` - Script projects with `mdk.ini` type=programmableblock
- `Mod` - Mod projects with appropriate references
- `Legacy` - MDK1 projects (auto-conversion supported)
- `Unknown` - Not an MDK project

### Analyzer Integration

**Independent Operation**: Analyzers run at compile-time in the IDE/build, separate from CLI tool
- PbAnalyzers: Validates against `pbwhitelist.dat` (embedded resource)
- ModAnalyzers: Validates against `modwhitelist.dat`

**Configuration Consumption**:
- Analyzers read `AdditionalFiles` (INI files automatically included by packager props)
- Consume compiler-visible properties: `Mdk-IgnorePaths`, `ProjectDir`
- Implement ignore patterns via `Microsoft.Extensions.FileSystemGlobbing`

**Custom MSBuild Target**: `AddNuGetDlls` (BeforeTargets="_GetPackageFiles")
- Merges package references with compiled DLLs
- Packages analyzer + dependencies into `analyzers/dotnet/cs` path (or `tools/netstandard2.0/cs` for MSBuild tasks)
- Uses `JoinItems` task to combine `ResolvedCompileFileDefinitions` and `PackageReference`
- Required for any NuGet package that includes custom MSBuild tasks or Roslyn analyzers with dependencies
- Dependencies must be marked with `PrivateAssets="all" IncludeInPackage="true"` in PackageReference

### Configuration System (INI Files)

**Naming Convention** (priority order):
1. `mdk.ini` / `mdk.local.ini` (new)
2. `{ProjectName}.mdk.ini` / `{ProjectName}.mdk.local.ini` (legacy)

**Scope**:
- `mdk.ini` - Project settings, checked into source control
- `mdk.local.ini` - Developer-specific overrides (e.g., custom binary paths), add to `.gitignore`

**Key Configuration Options**:
```ini
[mdk]
type=programmableblock                    # Project type
minify=none|trim|stripcomments|lite|full  # Minification level
ignores=obj/**/*,MDK/**/*,**/*.debug.cs   # Glob patterns for exclusion
namespaces=IngameScript                   # Allowed namespaces (comma-separated)
trace=on|off                              # Verbose output
```

**Binary Path Override** (for Space Engineers detection):
```ini
[mdk]
binarypath = C:\Path\To\SpaceEngineers\Bin64
```

### Minification Pipeline (Programmable Blocks Only)

**5-Tier Minification Levels**:
1. **none** - No processing, just combine files
2. **trim** - Remove unused types (not members)
3. **stripcomments** - trim + remove comments
4. **lite** - stripcomments + trim whitespace
5. **full** - lite + rename symbols to shorter names

**Processing Pipeline** (`ScriptProcessingManager`):
```
Preprocessors → Combine → Postprocessors → Compose → PostComposition → Produce
```

**Default Processors**:
- **Preprocessors**: Symbol analysis, #define detection
- **Postprocessors**: TypeTrimmer, CommentStripper, CodeSmallifier, WhitespaceTrimmer, SymbolRenamer
- **Composer**: Extracts `Program` class + helper types, flattens to single script
- **PostComposition**: Final validation, macro substitution
- **Producer**: Writes output with Instructions.readme and thumb.png

**Extensibility**: Pipeline accepts custom processors implementing `IDocumentProcessor` interfaces.

### Reference Resolution

**Mdk.References Package**:
- Automatically detects Space Engineers installation via registry, Steam library, or custom paths
- Generates MSBuild `.props` and `.targets` files dynamically at restore time
- References game DLLs directly from installation (no redistribution)

**For Unit Testing**: Set `<SpaceEngineersBinCopyLocal>true</SpaceEngineersBinCopyLocal>` to copy DLLs locally
- **Important**: Never redistribute these DLLs (Keen Software House copyright)

## Key Conventions

### Project Versioning
- `PackageVersion.txt` - Single source of truth for version numbers in each package directory
- Read via: `$([System.IO.File]::ReadAllText("$(MSBuildProjectDirectory)/PackageVersion.txt"))`
- All packages use same version for a release
- **Critical**: When modifying code in a project with `PackageVersion.txt`:
  1. Update `PackageVersion.txt` with the new version
  2. Add an entry to `ReleaseNotes.txt` describing the change
  3. CI/CD watches for changes to trigger deployments

### Template Structure
Templates in `ScriptTemplates/content/` follow this pattern:
- `.template.config/` - Visual Studio template metadata
- `mdk.ini` - Project configuration
- `Instructions.readme` - Injected as header comment in output
- `thumb.png` - 512x512 thumbnail for Workshop
- `Program.cs` - Entry point (must be in `IngameScript` namespace for PB scripts)

### Namespace Convention (Programmable Blocks)
- All script code should be in `IngameScript` namespace
- Game strips namespaces from final script - avoid type name collisions across namespaces
- Configure allowed namespaces in `mdk.ini`: `namespaces=IngameScript,MyHelpers`

### Ignore Patterns
Standard ignore patterns in `mdk.ini`:
- `obj/**/*` - Build output
- `MDK/**/*` - Legacy MDK1 files
- `**/*.debug.cs` - Debug-only helper files

### Commit Message Guidelines
Follow rules in `.hub-commit-message-rules.md`:
- User-facing descriptions (no implementation details, file paths, or class names)
- Focus on capabilities, not components
- Short subject line (<72 chars), brief body (1-3 sentences)
- Never list files changed or describe how new features work internally

### CI/CD Workflows
- **buildwithartefacts.yml** - Builds tools for win-x64, then builds packages
  - Triggers on push to `main` when `PackageVersion.txt` files change
  - Auto-publishes to NuGet on successful build
  - Artifacts: `mdk.exe`, `mdknotify-win.exe`, `checkdotnet.exe`
- **version-guard.yml** - Validates version and release notes on PRs
  - Ensures `PackageVersion.txt` is updated when code changes
  - Checks `ReleaseNotes.txt` includes the new version
  - Validates template package references are up-to-date

## Space Engineers Context

MDK2 targets the [Space Engineers](https://store.steampowered.com/app/244850/Space_Engineers/) game by Keen Software House:
- **Programmable Blocks**: In-game scripting with C# (subset of .NET, whitelist enforced)
- **Mods**: Full game modifications with broader API access
- **Whitelist Validation**: Analyzers prevent use of disallowed types/members
- Game installation required for development (assemblies referenced directly)

## Development Environment

- **Target Framework**: .NET 9.0 (CLI and tests), netstandard2.0 (analyzers)
- **IDE**: Visual Studio 2022 recommended (unconfirmed but suggested for stability)
- **Platform**: Windows-focused (win-x64 runtime), game only runs on Windows
- **Roslyn**: Microsoft.CodeAnalysis.CSharp.Workspaces 4.12.0

## Debugging and Testing MDK

For detailed information on debugging MDK itself, reproducing issues, and testing changes, see [debugging-mdk.md](debugging-mdk.md).

Quick reference:
- Build: `dotnet build Source\Mdk.CommandLine\Mdk.CommandLine.csproj -c Debug`
- Run: `& "Source\Mdk.CommandLine\bin\Debug\net9.0\win-x64\mdk.exe" pack "path\to\project.csproj"`
- Test projects: `Source\Mdk.CommandLine.Tests\TestData\`
- **Always test with working projects first** before investigating failures
