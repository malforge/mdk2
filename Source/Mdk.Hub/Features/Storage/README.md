# IFileStorageService Design

Comprehensive filesystem abstraction - paths + ALL file operations.

See `FILESYSTEM_PATH_MAP.md` in Mdk.Hub.Tests for full analysis and migration plan.

## Key Benefits
- 100% in-memory testing (no temp files)
- Parallel test execution (no conflicts)
- 10-100x faster tests
- Deterministic, isolated

## Interface
- Path: `GetApplicationDataPath()`, `GetTempPath()`, etc.
- Read: `ReadAllText()`, `FileExists()`, `OpenRead()`, etc.
- Write: `WriteAllText()`, `AppendAllText()`, `WriteAllBytes()`, etc.
- Directory: `CreateDirectory()`, `GetFiles()`, `DirectoryExists()`, etc.

Implementation types:
- **FileStorageService** - Production (real filesystem)
- **InMemoryFileStorageService** - Tests (memory only)
