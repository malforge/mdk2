# Filesystem Path Usage Map

## Summary
Mapping all uses of `Environment.GetFolderPath()` and `Path.GetTempPath()` to inform design of `IFileStorageService` abstraction.

---

## Production Code Usage

### Environment.GetFolderPath(ApplicationData) - %AppData% or ~/.config
**Purpose:** Persistent application data

| File | Purpose | Path Pattern |
|------|---------|-------------|
| `ProjectRegistry.cs:32` | Project registry storage | `%AppData%/MDK2/projects.json` |
| `Settings.cs:30` | Hub settings | `%AppData%/MDK2/settings.json` |
| `CommandLine/Shared/GlobalSettings.cs:14` | CLI settings | `%AppData%/MDK2/settings.json` |
| `ShellViewModel.cs:721` | Hub executable path marker | `%AppData%/MDK2/hub.path` |
| `HubLocator.cs:22` | CLI reads hub location | `%AppData%/MDK2/hub.path` |
| `AnnouncementService.cs:135` | Announcement cache | `%AppData%/MDK2/Hub/announcements.json` |
| `AboutViewModel.cs:83` | Open data folder action | `%AppData%/MDK2/Hub/` |
| `SpaceEngineersFinder.cs:33` | SE finder reads Hub settings | `%AppData%/MDK2/Hub/settings.json` |
| `SpaceEngineers.cs:50` (x4) | SE game data path | `%AppData%/SpaceEngineers/` |
| `ProjectService.cs:403,499` | Auto output path resolution | `%AppData%/SpaceEngineers/IngameScripts/local/` or `.../Mods/` |
| `ProjectInfoAction.cs:376` | Output path display | `%AppData%/SpaceEngineers/IngameScripts/local/` or `.../Mods/` |
| `Nuget.cs:101` | NuGet config search | `%AppData%/Nuget/NuGet.Config` |

### Environment.GetFolderPath(LocalApplicationData) - %LocalAppData% or ~/.local/share
**Purpose:** Non-roaming app data (logs, caches)

| File | Purpose | Path Pattern |
|------|---------|-------------|
| `FileLogger.cs:26` | Log file storage | `%LocalAppData%/MDK2/Hub/Logs/` |
| `AboutViewModel.cs:70` | Open logs folder action | `%LocalAppData%/MDK2/Hub/Logs/` |
| `InterProcessCommunication.cs:210` | IPC port file | `%LocalAppData%/MDK2/Hub/port.txt` |

### Environment.GetFolderPath(MyDocuments) - Documents folder
**Purpose:** User-facing default locations

| File | Purpose | Path Pattern |
|------|---------|-------------|
| `NewProjectDialogView.axaml.cs:34` | Default project location | `%MyDocuments%/Projects/` |
| `ProjectManagementAction.cs:134` | Create project dialog default | `%MyDocuments%` |

### Path.GetTempPath() - Temp directory
**Purpose:** Temporary downloads, installers

| File | Purpose | Path Pattern |
|------|---------|-------------|
| `UpdateManager.cs:282` | .NET SDK installer download (Windows) | `%Temp%/dotnet-sdk-9-installer.exe` |
| `UpdateManager.cs:325` | .NET install script download (Linux) | `%Temp%/dotnet-install.sh` |
| `HubLocator.cs:61` | Hub not found HTML error page | `%Temp%/mdk-hub-not-found.html` |
| `Extractor.cs:34` | SE extraction temp directory | `%Temp%/TempSEPath/` |

---

## Test Code Usage

### Path.GetTempPath() - Test isolation
**Purpose:** Temporary test directories/files (correct usage)

| File | Purpose |
|------|---------|
| `ProjectRegistryTests.cs:24,256` | Test registry files |
| `ProjectDetectorTests.cs:18` | Test project directories |
| `IniFileFinderTests.cs:17` | Test project directories |
| `IniFileNamingTests.cs:23` | Test project directories |
| `IniNamingIntegrationTests.cs:49` | Integration test directories |
| `LegacyConverterTests.cs:42` | Legacy conversion test directories |

---

## Proposed IFileStorageService Interface

```csharp
public interface IFileStorageService
{
    /// <summary>
    /// Gets the path for application data (roaming).
    /// Production: %AppData%/MDK2 or ~/.config/MDK2
    /// Test: returns mock/test directory
    /// </summary>
    string GetApplicationDataPath(params string[] subPaths);
    
    /// <summary>
    /// Gets the path for local application data (non-roaming).
    /// Production: %LocalAppData%/MDK2 or ~/.local/share/MDK2
    /// Test: returns mock/test directory
    /// </summary>
    string GetLocalApplicationDataPath(params string[] subPaths);
    
    /// <summary>
    /// Gets the path for temporary files.
    /// Production: %Temp% or /tmp
    /// Test: returns isolated test temp directory
    /// </summary>
    string GetTempPath(params string[] subPaths);
    
    /// <summary>
    /// Gets the user's documents folder.
    /// Production: %MyDocuments% or ~/Documents
    /// Test: returns mock documents directory
    /// </summary>
    string GetDocumentsPath(params string[] subPaths);
    
    /// <summary>
    /// Gets the Space Engineers data path.
    /// Production: %AppData%/SpaceEngineers
    /// Test: returns mock SE directory
    /// </summary>
    string GetSpaceEngineersDataPath(params string[] subPaths);
}
```

---

## Migration Strategy

### Phase 1: Create Service
1. Create `IFileStorageService` interface
2. Create `FileStorageService` (production implementation)
3. Create `TestFileStorageService` (test implementation with isolated temp directories)
4. Register in DI container

### Phase 2: Migrate High-Priority Classes (Testability Blockers)
- ✅ **ProjectRegistry** - Blocks Phase 1.2 tests (current issue!)
- ✅ **Settings** - Needed for settings tests
- ✅ **FileLogger** - Needed for logging tests

### Phase 3: Migrate Remaining Production Code
- ProjectService
- AnnouncementService
- UpdateManager
- ShellViewModel
- HubLocator
- SpaceEngineers (all 4 copies)
- etc.

### Phase 4: Update Test Code (Optional Cleanup)
- Tests can use TestFileStorageService instead of Path.GetTempPath() directly
- Provides consistency across all test isolation

---

## Benefits

1. **Testability**: Mock filesystem locations without reflection hacks
2. **Isolation**: Tests never touch real AppData
3. **Cross-platform**: Centralize platform-specific path logic
4. **Consistency**: Single source of truth for all path conventions
5. **Flexibility**: Easy to change path structure (e.g., add versioning: MDK2/v3/)

---

## Next Steps

1. Implement `IFileStorageService` + production implementation
2. Update `ProjectRegistry` to use service (fixes current test issue)
3. Update tests to use `TestFileStorageService`
4. Gradually migrate remaining classes

---

## Notes

- **SpaceEngineers classes**: 4 duplicates across projects (Mdk.Hub, Mdk.CommandLine, Mdk.References, Mdk.Extractor)
  - Should probably consolidate into shared library
  - Each would use IFileStorageService
  
- **Path.GetTempPath in tests**: Currently correct usage, but TestFileStorageService would provide better isolation

- **hub.path file**: Used for CLI → Hub communication
  - CLI writes hub location on startup
  - HubLocator reads to find running hub
  - Both need same service instance logic
