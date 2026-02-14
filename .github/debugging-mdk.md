# Debugging and Testing MDK

This guide explains how to reproduce, debug, and fix issues in MDK itself.

## Prerequisites

- MDK source code repository checked out
- .NET 9.0 SDK installed
- A test project to work with (either working or failing)

## Building MDK from Source

```bash
# From the Source directory
cd D:\Repos\Malforge\mdk2\Source

# Build in Debug mode for debugging
dotnet build Mdk.CommandLine\Mdk.CommandLine.csproj -c Debug

# Build in Release mode for testing
dotnet build Mdk.CommandLine\Mdk.CommandLine.csproj -c Release
```

The executable will be at:
- Debug: `Mdk.CommandLine\bin\Debug\net9.0\win-x64\mdk.exe`
- Release: `Mdk.CommandLine\bin\Release\net9.0\win-x64\mdk.exe`

## Running MDK Commands

### Method 1: Run the built executable directly (Recommended)
```bash
& "Mdk.CommandLine\bin\Debug\net9.0\win-x64\mdk.exe" pack "path\to\project.csproj"
& "Mdk.CommandLine\bin\Debug\net9.0\win-x64\mdk.exe" restore "path\to\project.csproj"
```

### Method 2: Run via dotnet run
```bash
dotnet run --project Mdk.CommandLine\Mdk.CommandLine.csproj -- pack "path\to\project.csproj"
dotnet run --project Mdk.CommandLine\Mdk.CommandLine.csproj -- restore "path\to\project.csproj"
```

**Important**: Always provide the full path to the `.csproj` file, not just the directory.

## Finding Test Projects

### Built-in Test Data
Test projects are available in:
```
Source\Mdk.CommandLine.Tests\TestData\
```

Example projects:
- `NewNamingTest` - Simple PB script project
- `NewNamingModTest` - Simple mod project

### Template Projects
Templates in `Source\ScriptTemplates\content\`:
- `0_Script` - Programmable Block script template
- `1_Mod` - Mod template
- `2_Mixin` - Shared project template

**Note**: Template projects may not be fully configured for testing. Test data projects are better for debugging.

## Debugging Process: Step-by-Step

### Step 0: Compare NuGet Package vs Current Source

**Critical First Step**: Determine if the bug exists in both the released package and current code.

1. **Test with the NuGet package version**:
   ```bash
   # Find the package mdk.exe
   $packageMdk = "C:\Users\lord-\.nuget\packages\mal.mdk2.pbpackager\VERSION\tools\win-x64\mdk.exe"
   
   # Test with exact parameters from MSBuild
   & $packageMdk pack "path\to\project.csproj" -configuration Debug -interactive
   ```

2. **Test with current source code**:
   ```bash
   # Build current code
   dotnet build Source\Mdk.CommandLine\Mdk.CommandLine.csproj -c Debug
   
   # Test with same parameters
   & "Source\Mdk.CommandLine\bin\Debug\net9.0\win-x64\mdk.exe" pack "path\to\project.csproj" -configuration Debug -interactive
   ```

3. **Compare results**:
   - ✅ **Bug in package, works in source**: Bug already fixed, just needs release
   - ✅ **Bug in both**: Bug needs to be fixed in current code
   - ✅ **Works in package, bug in source**: Regression - bisect to find when it broke
   - ✅ **Works in both**: Different reproduction steps needed

**Why this matters**: Don't waste time debugging code that's already been fixed!

### Step 1: Establish a Baseline with a Working Project

**Critical Rule**: Always verify normal operation before investigating failures.

1. Find or create a simple working test project
2. Run it successfully with the built MDK
3. Enable trace mode to see execution flow
4. Document what success looks like

Example:
```bash
# Run with trace enabled
& "Mdk.CommandLine\bin\Debug\net9.0\win-x64\mdk.exe" pack "TestData\NewNamingTest\NewNamingTest.csproj" --trace
```

### Step 2: Reproduce the Issue

1. Use the failing project/scenario
2. Use shorter timeouts (15-30 seconds) to detect hangs quickly
3. Capture output to a file if needed
4. Note the exact command and parameters used

Example:
```bash
# Test for freeze (max 30 seconds)
$output = & "Mdk.CommandLine\bin\Debug\net9.0\win-x64\mdk.exe" pack "path\to\failing\project.csproj" 2>&1
$output | Out-File "error-output.txt"
```

### Step 3: Compare Behaviors

Compare the failing case against the working baseline:
- What's different about the project structure?
- What's different about the output?
- Where does execution diverge?

## Debugging Techniques

### Enable Trace Mode

Add `--trace` flag to see verbose output:
```bash
& "mdk.exe" pack "project.csproj" --trace
```

Or modify `mdk.ini` in the test project:
```ini
[mdk]
trace=on
```

### Use a Debugger

#### Visual Studio
1. Open `MDK-Complete.sln` or `MDK-Packages.sln`
2. Set `Mdk.CommandLine` as startup project
3. Right-click project → Properties → Debug → General → Open debug launch profiles UI
4. Set command line arguments: `pack D:\path\to\project.csproj`
5. Set breakpoints in relevant code
6. Press F5 to debug

#### VS Code
1. Open the repository in VS Code
2. Create `.vscode/launch.json`:
```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Debug MDK Pack",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/Source/Mdk.CommandLine/bin/Debug/net9.0/win-x64/mdk.exe",
            "args": ["pack", "D:\\path\\to\\project.csproj"],
            "cwd": "${workspaceFolder}/Source",
            "console": "internalConsole",
            "stopAtEntry": false
        }
    ]
}
```

### Attach to a Running Process

If the process hangs:
1. Find the process ID: `Get-Process mdk`
2. Attach debugger to the PID
3. Pause execution to see where it's stuck
4. Check call stack and thread state

### Check for Infinite Loops

Signs of an infinite loop:
- Process doesn't exit
- Same output repeating
- CPU usage remains high
- No progress after reasonable timeout

To diagnose:
1. Attach debugger and pause execution
2. Check the call stack
3. Look for recursive calls or while loops

## Common Debugging Scenarios

### Process hangs/freezes
1. Use short timeouts (15-30 seconds) to detect
2. Attach debugger and pause execution
3. Check call stack and thread state
4. Look for infinite loops or deadlocks

### Compilation errors in test projects
1. Ensure Space Engineers is installed
2. Run `mdk restore` first to set up references
3. Check that package references are correct

### Different behavior between manual and MSBuild execution
1. Check MSBuild integration files (`.props` files)
2. Capture exact command MSBuild uses with `-v:detailed`
3. Reproduce with those exact parameters

## Best Practices

1. **Always test NuGet package vs current source first** - Don't debug already-fixed bugs!
2. **Test with working projects first** - Establish baseline behavior before investigating failures
3. **Always specify .csproj file path** - Don't rely on directory auto-detection
4. **Use trace mode liberally** - Helps understand execution flow (if version supports it)
5. **Keep timeouts short** - 15-30 seconds is enough to detect hangs
6. **Stop runaway processes immediately** - Don't waste time waiting
7. **Document findings in `.issues/` folder** - One markdown file per issue
8. **Compare working vs failing** - Look for differences in project structure or configuration

## Useful PowerShell Commands

```powershell
# Build MDK
dotnet build Mdk.CommandLine\Mdk.CommandLine.csproj -c Debug

# Run with timeout and capture output
$output = & "mdk.exe" pack "project.csproj" 2>&1 | Select-Object -First 20

# Find all MDK test projects
Get-ChildItem -Recurse -Filter "mdk.ini" | Select-Object Directory

# Kill hung MDK process
Get-Process mdk | Stop-Process -Force

# Check if process exited
$proc = Start-Process mdk.exe -ArgumentList "pack", "project.csproj" -PassThru -NoNewWindow
Start-Sleep -Seconds 5
$proc.HasExited
```

## Creating Test Projects

To create a minimal test project:

1. Create a directory for the test
2. Copy a working `.csproj` from test data
3. Create minimal `Program.cs`
4. Create `mdk.ini` and `mdk.local.ini`
5. Run `mdk restore` to set up references
6. Test with `mdk pack`

## Documenting Issues

When you find a bug or need to investigate an issue:

1. Create a new file in `.issues/` folder: `.issues/descriptive-name.md`
2. Include:
   - **Minimal reproduction steps** - Simplest way to trigger the bug
   - **Symptoms** - What actually happens
   - **Expected behavior** - What should happen
   - **Status** - Investigation progress, fixes needed
   - **Testing comparison** - NuGet package vs current source results
3. Keep issue documents focused on one specific problem
4. Update as investigation progresses

**Important**: The `.issues/` folder is gitignored. Use it for:
- Issue investigation documents
- Temporary test outputs and logs
- Build output captures
- Diagnostic files
- Any other temporary files generated during debugging

Example:
```bash
# Redirect build output to .issues folder
dotnet build project.csproj -v:detailed > .issues/build-output.txt 2>&1

# Save trace logs for analysis
mdk.exe pack project.csproj --trace > .issues/trace-log.txt 2>&1

# Keep test projects or minimal reproductions
mkdir .issues/test-case
```

## Related Files

- **Program.cs** - Main entry point, command routing, exception handling
- **Parameters.cs** - Command line argument parsing
- **MdkProject.cs** - Project type detection and loading
- **IngameScript/Pack/** - PB script packaging logic
- **Mod/Pack/** - Mod packaging logic
- **DirectConsole.cs** - Console output implementation
