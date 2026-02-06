# Mdk.Hub Testing Plan

## Current State
- **Test Framework**: NUnit 4.3.2 + FakeItEasy 8.3.0
- **Existing Coverage**: ~2 test files (IniTests ✅, ConfigurationNormalizationTests ✅)
- **Gap**: Core services and business logic largely untested

## Testing Philosophy

### What We Test (High Value)
- ✅ **Services with business logic** - Core orchestration and decision-making
- ✅ **Data persistence/loading** - ProjectRegistry, configuration layers
- ✅ **Validation logic** - Project detection, configuration validation
- ✅ **Complex algorithms** - Update checking, version comparisons, layer merging
- ✅ **ViewModels with logic** - Filtering, search, validation, state management

### What We Don't Test (Low Value)
- ❌ **Pure presentation ViewModels** - Simple property bindings
- ❌ **UI coordination services** - SnackbarService, ToastService
- ❌ **Third-party integrations** - Velopack internals, actual HTTP calls
- ❌ **Platform-specific UI** - Avalonia controls, window management

---

## Phased Implementation

### **Phase 1: Core Project Management** (HIGH PRIORITY)
Foundation of the entire Hub - project detection, loading, and persistence.

#### 1.1 ProjectDetector Tests (~8 tests, 45-60 min) ✅ COMPLETE
**File**: `Features/Projects/ProjectDetectorTests.cs`

- [x] `TryDetectProject_ValidProject_ReturnsTrue()`
- [x] `TryDetectProject_WithMdkIni_DetectsCorrectly()`
- [x] `TryDetectProject_WithMdkLocalIni_DetectsCorrectly()`
- [x] `TryDetectProject_WithBothIniFiles_DetectsCorrectly()`
- [x] `TryDetectProject_MissingCsproj_ReturnsFalse()`
- [x] `TryDetectProject_NotMdkProject_ReturnsFalse()`
- [x] `TryDetectProject_CorruptXml_HandleGracefully()`
- [x] `TryDetectProject_InvalidPath_ReturnsFalse()`
- [x] `TryDetectProject_ModProject_DetectsModType()` (bonus test)
- [x] `TryDetectProject_DefaultsToProgrammableBlock_WhenPackageAmbiguous()` (bonus test)

**Testing Strategy:**
- ✅ Uses temp directories with real file structures
- ✅ Tests both positive and negative cases
- ✅ Verifies error handling for corrupt/invalid inputs
- ✅ All 10 tests passing

---

#### 1.2 ProjectRegistry Tests (~10 tests, 60-75 min)
**File**: `Features/Projects/ProjectRegistryTests.cs`

- [ ] `GetProjects_EmptyRegistry_ReturnsEmptyList()`
- [ ] `AddProject_NewProject_AddsSuccessfully()`
- [ ] `AddProject_DuplicatePath_DoesNotAddAgain()`
- [ ] `RemoveProject_ExistingProject_RemovesSuccessfully()`
- [ ] `RemoveProject_NonExistentProject_DoesNotThrow()`
- [ ] `LoadFromFile_ValidJson_LoadsProjects()`
- [ ] `LoadFromFile_MissingFile_CreatesEmpty()`
- [ ] `LoadFromFile_CorruptJson_HandleGracefully()`
- [ ] `SaveToFile_WithProjects_PersistsCorrectly()`
- [ ] `LastReferenced_UpdatesOnProjectAccess()`

**Testing Strategy:**
- Mock file system OR use temp files
- Test JSON serialization round-trips
- Verify ProjectsChanged event fires correctly

---

#### 1.3 ProjectService Core Tests (~12 tests, 90-120 min)
**File**: `Features/Projects/ProjectServiceTests.cs`

**Configuration Loading:**
- [ ] `LoadProjectDataAsync_ValidProject_LoadsConfiguration()`
- [ ] `LoadProjectDataAsync_OnlyMainIni_LoadsCorrectly()`
- [ ] `LoadProjectDataAsync_OnlyLocalIni_LoadsCorrectly()`
- [ ] `LoadProjectDataAsync_BothInis_MergesCorrectly()`
- [ ] `LoadProjectDataAsync_NonExistentProject_ReturnsNull()`
- [ ] `LoadProjectDataAsync_CorruptIni_HandleGracefully()`

**Configuration Saving:**
- [ ] `SaveProjectDataAsync_ValidData_SavesCorrectly()`
- [ ] `SaveProjectDataAsync_UpdatesLayerPrecedence_PreservesComments()`

**Project Management:**
- [ ] `TryAddProject_ValidProject_AddsAndFiresEvent()`
- [ ] `TryAddProject_InvalidProject_ReturnsFalse()`
- [ ] `NavigateToProject_ExistingProject_UpdatesState()`
- [ ] `NavigateToProject_NonExistentProject_ReturnsFalse()`

**Testing Strategy:**
- Mock IProjectRegistry, IUpdateManager, IShell, INuGetService
- Use temp directories for real file I/O on config files
- Verify events fire with correct parameters
- Test error paths and edge cases

---

### **Phase 2: Update Management** (HIGH PRIORITY)
Critical for keeping users on latest versions.

#### 2.1 UpdateManager Tests (~10 tests, 75-90 min)
**File**: `Features/Updates/UpdateManagerTests.cs`

**Update Checking:**
- [ ] `CheckForUpdatesAsync_HubUpdateAvailable_NotifiesCorrectly()`
- [ ] `CheckForUpdatesAsync_TemplateUpdateAvailable_NotifiesCorrectly()`
- [ ] `CheckForUpdatesAsync_ProjectPackagesOutdated_NotifiesCorrectly()`
- [ ] `CheckForUpdatesAsync_AllUpToDate_NoNotifications()`
- [ ] `CheckForUpdatesAsync_NetworkError_HandleGracefully()`

**Update Execution:**
- [ ] `UpdateTemplatesAsync_Success_ReturnsTrue()`
- [ ] `UpdateTemplatesAsync_Failure_ReturnsFalse()`
- [ ] `UpdateProjectPackagesAsync_Success_UpdatesAllProjects()`
- [ ] `UpdateProjectPackagesAsync_PartialFailure_ContinuesProcessing()`

**Version Comparisons:**
- [ ] `VersionInfo_Comparison_HandlesSemanticVersioning()`

**Testing Strategy:**
- Mock IGitHubService, INuGetService to return test versions
- Mock Process.Start for template updates
- Use FakeItEasy to verify callback invocations
- Test semantic version parsing edge cases

---

#### 2.2 ProjectUpdateChecker Tests (~8 tests, 60-75 min)
**File**: `Features/Projects/ProjectUpdateCheckerTests.cs`

- [ ] `QueueCheck_SingleProject_ChecksAndNotifies()`
- [ ] `QueueCheck_MultipleProjects_ProcessesInOrder()`
- [ ] `QueueCheck_SameProjectTwice_DeduplicatesQueue()`
- [ ] `CheckAllProjects_WithOutdatedPackages_FiresEvents()`
- [ ] `CheckAllProjects_AllUpToDate_NoEvents()`
- [ ] `CheckProject_NetworkError_HandlesGracefully()`
- [ ] `VersionComparison_PrereleaseSuffix_ComparesCorrectly()`
- [ ] `CooldownPeriod_RecentCheck_SkipsCheck()`

**Testing Strategy:**
- Mock IProjectService to return test projects
- Mock INuGetService for version checks
- Verify throttling/cooldown logic
- Test async queue processing

---

### **Phase 3: Configuration System** (MEDIUM-HIGH PRIORITY)
Already partially tested, but needs completeness.

#### 3.1 ProjectData Layer Tests (~6 tests, 45-60 min)
**File**: `Features/Projects/Configuration/ProjectDataTests.cs`

- [ ] `GetEffective_OnlyDefault_ReturnsDefault()`
- [ ] `GetEffective_MainOverridesDefault_ReturnsMain()`
- [ ] `GetEffective_LocalOverridesAll_ReturnsLocal()`
- [ ] `GetEffective_PartialOverrides_MergesCorrectly()`
- [ ] `HasOverrides_LocalSetDifferently_ReturnsTrue()`
- [ ] `HasOverrides_LocalMatchesEffective_ReturnsFalse()`

**Note**: ConfigurationNormalizationTests already covers key movement.

---

### **Phase 4: ViewModels with Logic** (MEDIUM PRIORITY)
Test ViewModels that have actual logic beyond simple bindings.

#### 4.1 ProjectOverviewViewModel Tests (~8 tests, 60-75 min)
**File**: `Features/Projects/Overview/ProjectOverviewViewModelTests.cs`

**Search/Filter:**
- [ ] `SearchText_FiltersProjects_ByName()`
- [ ] `SearchText_FiltersProjects_ByPath()`
- [ ] `SearchText_ThrottlesSearch_DoesNotSpam()`
- [ ] `SearchText_Empty_ShowsAllProjects()`

**Selection:**
- [ ] `SelectedProject_ChangesState_UpdatesProjectService()`
- [ ] `SelectedProject_ProjectRemoved_ClearsSelection()`

**Refresh:**
- [ ] `RefreshCommand_ReloadsProjects_FromRegistry()`
- [ ] `ProjectAdded_UpdatesList_AddsNewProject()`

**Testing Strategy:**
- Mock IProjectService, IUpdateManager
- Use ObservableCollection assertions
- Test throttling behavior with delays

---

#### 4.2 ProjectOptionsViewModel Tests (~10 tests, 75-90 min)
**File**: `Features/Projects/Options/ProjectOptionsViewModelTests.cs`

**Dirty State Tracking:**
- [ ] `HasChanges_ModifyingProperty_SetsDirty()`
- [ ] `HasChanges_SaveCommand_ClearsDirty()`
- [ ] `HasChanges_CancelCommand_RevertChanges()`
- [ ] `HasChanges_MultipleEdits_TracksCorrectly()`

**Override Indicators:**
- [ ] `HasLocalOverride_LocalSetDifferently_ShowsIndicator()`
- [ ] `HasLocalOverride_LocalMatchesMain_HidesIndicator()`
- [ ] `ClearOverride_ResetsToMainValue_UpdatesUI()`

**Validation:**
- [ ] `Validate_InvalidConfiguration_ShowsErrors()`
- [ ] `Validate_ValidConfiguration_NoErrors()`

**Save/Cancel:**
- [ ] `SaveCommand_PersistsChanges_CallsProjectService()`

**Testing Strategy:**
- Mock IProjectService for save operations
- Create test ProjectData with known override states
- Verify INotifyPropertyChanged events

---

#### 4.3 NewProjectDialogViewModel Tests (~6 tests, 45-60 min)
**File**: `Features/Projects/NewProjectDialog/NewProjectDialogViewModelTests.cs`

- [ ] `CreateCommand_ValidInputs_CreatesProject()`
- [ ] `CreateCommand_InvalidName_ShowsError()`
- [ ] `CreateCommand_InvalidPath_ShowsError()`
- [ ] `CreateCommand_ExistingDirectory_ShowsError()`
- [ ] `Validation_EmptyName_DisablesCreate()`
- [ ] `Validation_AllValid_EnablesCreate()`

---

### **Phase 5: Utilities & Helpers** (LOWER PRIORITY)
Supporting code that still benefits from tests.

#### 5.1 CanonicalPath Tests (~8 tests, 45-60 min)
**File**: `Utility/CanonicalPathTests.cs`

- [ ] `Constructor_NormalizesPath_HandlesForwardSlashes()`
- [ ] `Constructor_NormalizesPath_HandlesBackslashes()`
- [ ] `Equals_SamePath_ReturnsTrue()`
- [ ] `Equals_DifferentCase_ReturnsTrue()` (Windows)
- [ ] `Equals_DifferentCase_ReturnsFalse()` (Linux)
- [ ] `Equals_DifferentPath_ReturnsFalse()`
- [ ] `IsEmpty_EmptyPath_ReturnsTrue()`
- [ ] `ToString_ReturnsOriginalPath()`

---

#### 5.2 CollectionExtensions Tests (~4 tests, 20-30 min)
**File**: `Utility/CollectionExtensionsTests.cs`

- [ ] `ReplaceWith_NewItems_UpdatesCollection()`
- [ ] `ReplaceWith_Empty_ClearsCollection()`
- [ ] `ReplaceWith_Null_ThrowsException()`
- [ ] `ReplaceWith_SameItems_MaintainsOrder()`

---

### **Phase 6: Integration & Edge Cases** (LOWEST PRIORITY)
Full workflows and uncommon scenarios.

#### 6.1 End-to-End Scenarios (~5 tests, 60-90 min)
**File**: `Integration/ProjectLifecycleTests.cs`

- [ ] `FullWorkflow_CreateLoadEditSave_WorksCorrectly()`
- [ ] `FullWorkflow_AddProjectCheckUpdatesInstall_WorksCorrectly()`
- [ ] `FullWorkflow_NormalizeConfiguration_MovesKeysCorrectly()`
- [ ] `FullWorkflow_MultipleProjects_HandlesCorrectly()`
- [ ] `FullWorkflow_ConcurrentOperations_ThreadSafe()`

---

## Test Infrastructure

### Mock Strategy

**Always Mock (External Dependencies):**
- `IGitHubService` → return fixed VersionInfo
- `INuGetService` → return fixed NuGet versions
- `ILogger` → capture logs for assertions
- `Process.Start` → verify command-line args only

**Sometimes Mock (Internal Coordination):**
- `IProjectRegistry` → use in-memory list for unit tests, real for integration
- `ISettings` → use in-memory dictionary
- `IShell` → mock window interactions

**Never Mock (Business Logic):**
- `Ini` parser (use real parser)
- `CanonicalPath` (use real implementation)
- Configuration layer classes (test actual merging)

### Test Helpers

**Recommended Utilities:**
```csharp
// Helper for creating test project structures
public class TestProjectBuilder
{
    public TestProjectBuilder WithMainIni(string content) { ... }
    public TestProjectBuilder WithLocalIni(string content) { ... }
    public TestProjectBuilder InDirectory(string path) { ... }
    public string Build() { ... }
}

// Helper for mocking services
public static class MockFactory
{
    public static IProjectRegistry CreateMockRegistry(params ProjectInfo[] projects) { ... }
    public static INuGetService CreateMockNuGetService(Dictionary<string, string> versions) { ... }
}

// Temp directory management
public class TempDirectory : IDisposable
{
    public string Path { get; }
    public void Dispose() => Directory.Delete(Path, true);
}
```

---

## Estimated Effort

| Phase | Tests | Time Estimate |
|-------|-------|---------------|
| **Phase 1: Core Project Management** | ~30 tests | 3.5-4.5 hours |
| **Phase 2: Update Management** | ~18 tests | 2.5-3 hours |
| **Phase 3: Configuration System** | ~6 tests | 0.75-1 hour |
| **Phase 4: ViewModels with Logic** | ~24 tests | 3-4 hours |
| **Phase 5: Utilities & Helpers** | ~12 tests | 1.5-2 hours |
| **Phase 6: Integration & Edge Cases** | ~5 tests | 1-1.5 hours |
| **TOTAL** | **~95 tests** | **12-16 hours** |

---

## Success Metrics

### Coverage Targets
- **Services**: 80%+ code coverage on business logic
- **Business Logic Classes**: 85%+ coverage
- **ViewModels**: 60%+ coverage (focus on logic, not bindings)
- **Utilities**: 90%+ coverage

### Quality Indicators
- All tests run in <5 seconds
- No flaky tests (time-dependent, file system races)
- Clear test names following pattern: `MethodName_Scenario_ExpectedResult`
- Comprehensive arrange/act/assert structure

---

## Progress Tracking

### Phase 1: Core Project Management
- [ ] 1.1 ProjectDetector Tests (8 tests)
- [ ] 1.2 ProjectRegistry Tests (10 tests)
- [ ] 1.3 ProjectService Core Tests (12 tests)

### Phase 2: Update Management
- [ ] 2.1 UpdateManager Tests (10 tests)
- [ ] 2.2 ProjectUpdateChecker Tests (8 tests)

### Phase 3: Configuration System
- [ ] 3.1 ProjectData Layer Tests (6 tests)

### Phase 4: ViewModels with Logic
- [ ] 4.1 ProjectOverviewViewModel Tests (8 tests)
- [ ] 4.2 ProjectOptionsViewModel Tests (10 tests)
- [ ] 4.3 NewProjectDialogViewModel Tests (6 tests)

### Phase 5: Utilities & Helpers
- [ ] 5.1 CanonicalPath Tests (8 tests)
- [ ] 5.2 CollectionExtensions Tests (4 tests)

### Phase 6: Integration & Edge Cases
- [ ] 6.1 End-to-End Scenarios (5 tests)

---

## Notes

- Each phase is independent and can be implemented in any order
- Phase 1-2 are highest priority (core functionality)
- Consider continuous integration: run tests on every commit
- Maintain this document as tests are added
- Update estimates based on actual time spent
