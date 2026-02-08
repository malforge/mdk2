# Mdk.Hub.UITests Testing Plan

## Current State
- **Test Framework**: Avalonia.Headless.NUnit 11.3.11 + NUnit 4.3.2
- **Existing Coverage**: PathInput control (14 tests ✅)
- **Gap**: Other custom controls untested

## Testing Philosophy

### What We Test (High Value UI)
- ✅ **Custom controls with logic** - Validation, state management, user interactions
- ✅ **Template part interactions** - Buttons, textboxes, compound behaviors
- ✅ **Property change reactions** - Bindings, computed properties, visual states
- ✅ **User input flows** - Keyboard, mouse, focus management
- ✅ **Cross-platform behavior** - Windows vs Unix path handling

### What We Don't Test (Low Value UI)
- ❌ **Simple property bindings** - No logic, just pass-through
- ❌ **Pure styling** - Visual appearance, colors, sizes
- ❌ **Layout positioning** - Grid/Stack panel arrangement
- ❌ **Third-party controls** - Avalonia built-ins, FluentTheme
- ❌ **Platform dialogs** - Folder picker, file dialogs (mocked in integration tests)

---

## Test Infrastructure

### Testable Control Pattern
For testing platform-specific behavior (like path validation), we use a pattern of overridable virtual methods:

```csharp
public class PathInput : TemplatedControl
{
    protected virtual bool IsWindowsPlatform() => OperatingSystem.IsWindows();
}

public class TestablePathInput : PathInput
{
    public bool? ForcedPlatform { get; set; }
    protected override bool IsWindowsPlatform() => ForcedPlatform ?? base.IsWindowsPlatform();
}
```

This allows testing both Windows and Unix behavior regardless of host OS.

### Test Theme
`Theme/TestTheme.axaml` provides minimal control templates for testing without app dependencies:
- No colors/brushes from main app
- No custom fonts or resources
- Just functional template parts (PART_TextBox, PART_Button, etc.)
- Includes both base and testable control variants

### Test Application
`TestApp.axaml` + `TestAppBuilder.cs` set up the headless environment:
- FluentTheme for base styles
- TestTheme for custom controls
- IconProvider registration (FontAwesome)
- Proper dispatcher and layout initialization

---

## Phase 1: PathInput Control (COMPLETE ✅)

**File**: `Framework/Controls/PathInputTests.cs`
**Status**: 14/14 tests passing
**Time Spent**: ~3-4 hours (includes infrastructure setup)

### Tests Implemented
- [x] `PathInput_InitializesWithEmptyPath()` - Default state
- [x] `PathInput_AcceptsEmptyPathWhenNoDefault()` - Empty validation
- [x] `PathInput_RejectsEmptyPathWhenDefaultExists()` - Required field behavior
- [x] `PathInput_RejectsInvalidCharacters()` - Character validation
- [x] `PathInput_NormalizesWindowsPathOnCommit()` - Windows path normalization
- [x] `PathInput_NormalizesUnixPathOnCommit()` - Unix path normalization
- [x] `PathInput_WindowsRejectsReservedNames()` - CON, PRN, AUX, etc.
- [x] `PathInput_WindowsRejectsMultipleColons()` - Colon position validation
- [x] `PathInput_AllowsDefaultValueWithoutValidation()` - Sentinel values (e.g., "auto")
- [x] `PathInput_ResetButtonRestoresDefault()` - Reset functionality
- [x] `PathInput_ResetButtonHiddenWhenCanResetFalse()` - Visibility logic
- [x] `PathInput_CommitsOnEnterKey()` - Keyboard interaction
- [x] `PathInput_RejectsPathTooLong()` - 4096 char limit
- [x] `PathInput_UpdatesTextBoxWhenPathSetExternally()` - External property changes

### Key Achievements
✅ Found and fixed 2 real bugs (multiple colons, reset button visibility)
✅ Tests validate requirements, not accidents
✅ Cross-platform testing works on any OS
✅ Comprehensive coverage of validation logic

---

## Phase 2: DateTimeDisplay Control (RECOMMENDED)

## Summary & Recommendations

### Current State
✅ **PathInput**: Fully tested (14 tests)
❓ **DateTimeDisplay**: Untested - RECOMMENDED to add

### Recommended Next Steps

**Option A: Add DateTimeDisplay Tests (Recommended)**
- Medium value - has actual logic to test
- Moderate effort (~1 hour)
- Would complete testing of all logic-heavy controls
- **Estimated**: 8 tests, 60-75 minutes

**Option B: Stop Here (Also Valid)**
- PathInput was the most complex control
- Other controls are simpler (less logic, more styling)
- Focus testing effort on unit tests (business logic) instead
- UI tests are slower and more brittle than unit tests

**My Recommendation**: Add DateTimeDisplay tests, skip the others. The cost/benefit ratio drops significantly after that.

---

## Test Quality Metrics

### Current Metrics (PathInput)
- ✅ **14/14 tests passing** (100%)
- ✅ **Cross-platform testing** works on any OS
- ✅ **Fast execution** (~800ms for full suite)
- ✅ **Clear test names** follow pattern: `Control_Scenario_ExpectedResult`
- ✅ **Bug detection** found 2 real bugs during development
- ✅ **Requirements-based** tests validate behavior, not implementation

### Success Criteria for New Tests
- All tests pass consistently (no flaky tests)
- Test execution under 1 second per control
- Clear arrange/act/assert structure
- Test actual requirements, not implementation details
- Cover both happy path and error cases
- Include cross-platform scenarios where applicable

---

## Notes

- **Headless testing is powerful** but limited to non-visual aspects
- **Focus on controls with logic** - validation, state management, user flows
- **Skip pure styling tests** - those are better validated manually/visually
- **Platform-specific logic** needs testable abstractions (virtual methods)
- **Consider maintenance cost** - UI tests are more brittle than unit tests
- **Test themes must be self-contained** - no app dependencies

### Maintenance Reminders
- Update TestTheme.axaml when adding new testable controls
- Keep TestablePathInput pattern for platform-specific controls
- Document any new testing patterns in this file
- Review test names for clarity when adding new tests

---

## Time Investment Summary

| Component | Tests | Status | Time Spent | Value |
|-----------|-------|--------|------------|-------|
| **PathInput** | 14 | ✅ Complete | 3-4 hours | ⭐⭐⭐⭐⭐ Very High |
| **DateTimeDisplay** | 8 | ⏸️ Proposed | ~1 hour | ⭐⭐⭐ Medium |
| **Infrastructure** | - | ✅ Complete | 1-2 hours | ⭐⭐⭐⭐⭐ Very High |
| **TOTAL** | **14 (+8?)** | - | **4-5 hours (+1?)** | - |

**Conclusion**: Current test coverage is excellent for the controls that matter. Adding DateTimeDisplay tests would be nice-to-have but not critical. The testing infrastructure is solid and can support additional controls if bugs are discovered.
