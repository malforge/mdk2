---
name: debugging-mdk
description: Use when reproducing, debugging, or fixing issues in MDK itself — building mdk.exe from source, running it against a test project, comparing the released NuGet package against current source, attaching a debugger, diagnosing hangs/freezes, or capturing trace output. Trigger on user requests like "reproduce this MDK bug", "debug the packager", "why does pack hang on this project", "is this fixed in source already", or when investigating MDK CLI behavior that differs between manual and MSBuild execution.
---

# Debugging and Testing MDK

This skill walks through reproducing, debugging, and fixing issues in MDK itself (the CLI tool, packagers, and analyzers — not user scripts).

## Prerequisites

- MDK source repo checked out (working directory: `D:\Repos\Malforge\mdk2`)
- .NET 9.0 SDK installed
- A test project to work with (a working baseline AND, ideally, a failing repro)

## Building MDK from source

From `Source\`:

```bash
# Debug build (use this for debugging)
dotnet build Mdk.CommandLine\Mdk.CommandLine.csproj -c Debug

# Release build (use this for parity testing)
dotnet build Mdk.CommandLine\Mdk.CommandLine.csproj -c Release
```

Built executable:
- Debug: `Source\Mdk.CommandLine\bin\Debug\net9.0\win-x64\mdk.exe`
- Release: `Source\Mdk.CommandLine\bin\Release\net9.0\win-x64\mdk.exe`

## Running MDK commands

**Preferred** — invoke the built exe directly so behavior matches what users see:

```bash
& "Source\Mdk.CommandLine\bin\Debug\net9.0\win-x64\mdk.exe" pack "path\to\project.csproj"
& "Source\Mdk.CommandLine\bin\Debug\net9.0\win-x64\mdk.exe" restore "path\to\project.csproj"
```

Alternative — `dotnet run` (slower, but skips the build/run split):

```bash
dotnet run --project Source\Mdk.CommandLine\Mdk.CommandLine.csproj -- pack "path\to\project.csproj"
```

**Always pass the full `.csproj` path.** Do not rely on directory auto-detection — it masks real bugs and produces inconsistent repros.

## Test projects

Built-in test data: `Source\Mdk.CommandLine.Tests\TestData\`
- `NewNamingTest` — simple PB script project
- `NewNamingModTest` — simple mod project

Templates: `Source\ScriptTemplates\content\`
- `0_Script`, `1_Mod`, `2_Mixin`

Templates are not always fully configured for direct testing — prefer `TestData\` projects when you need a known-good baseline.

## Debugging process

### Step 0 — Compare NuGet package vs. current source (do this FIRST)

Determine whether the bug exists only in the released package, only in source, or both — before spending time debugging.

```bash
# Released package mdk.exe (find under user's nuget cache)
$packageMdk = "C:\Users\$env:USERNAME\.nuget\packages\mal.mdk2.pbpackager\<VERSION>\tools\win-x64\mdk.exe"
& $packageMdk pack "path\to\project.csproj" -configuration Debug -interactive

# Current source mdk.exe
& "Source\Mdk.CommandLine\bin\Debug\net9.0\win-x64\mdk.exe" pack "path\to\project.csproj" -configuration Debug -interactive
```

Interpret the matrix:
- **Bug in package, works in source** → already fixed; just needs a release.
- **Bug in both** → fix in current code.
- **Works in package, bug in source** → regression; bisect.
- **Works in both** → repro is incomplete; gather more info.

Skipping this step is the most common way to waste an hour on an already-fixed bug.

### Step 1 — Establish a baseline with a working project

Always verify normal operation before investigating failures.

1. Pick a known-good project (e.g. `TestData\NewNamingTest`).
2. Run it successfully with the built mdk.exe.
3. Enable trace mode to see the execution flow.
4. Note what success looks like (exit code, output shape, files written).

```bash
& "Source\Mdk.CommandLine\bin\Debug\net9.0\win-x64\mdk.exe" pack "Source\Mdk.CommandLine.Tests\TestData\NewNamingTest\NewNamingTest.csproj" --trace
```

### Step 2 — Reproduce the issue

1. Use the failing project/scenario.
2. Use **short timeouts (15–30 seconds)** to detect hangs quickly. Do not wait minutes.
3. Capture output to a file under `.issues/` if you'll need it for analysis.
4. Note the exact command + parameters used.

```bash
$output = & "Source\Mdk.CommandLine\bin\Debug\net9.0\win-x64\mdk.exe" pack "path\to\failing\project.csproj" 2>&1
$output | Out-File ".issues\error-output.txt"
```

### Step 3 — Compare behaviors

Diff the failing case against the working baseline:
- Project structure differences?
- Output divergence point?
- Configuration (`mdk.ini`) differences?

## Debugging techniques

### Trace mode

Add `--trace` to the command:
```bash
& "mdk.exe" pack "project.csproj" --trace
```

Or set it in the test project's `mdk.ini`:
```ini
[mdk]
trace=on
```

### Visual Studio debugger

1. Open `Source\MDK-Complete.sln` (or `MDK-Packages.sln`).
2. Set `Mdk.CommandLine` as startup project.
3. Project → Properties → Debug → "Open debug launch profiles UI".
4. Set command line arguments, e.g. `pack D:\path\to\project.csproj`.
5. Set breakpoints, F5.

### VS Code debugger

Create `.vscode/launch.json`:

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

### Attaching to a running/hung process

```powershell
Get-Process mdk
# Attach debugger to the PID, pause execution, inspect call stack and threads
```

Signs of an infinite loop: process never exits, repeating output, sustained CPU, no progress past the timeout. Attach + pause + inspect call stack — look for recursion or unbounded `while`.

## Common scenarios

**Process hangs/freezes**
- Use 15–30s timeouts to detect.
- Attach + pause + inspect call stack.
- Look for infinite loops or deadlocks (esp. around I/O, prompts, or lock acquisition).

**Compilation errors in test projects**
- Confirm Space Engineers is installed (references resolve from the game install).
- Run `mdk restore` first to set up references.
- Check the package references in the `.csproj`.

**Behavior differs between manual and MSBuild execution**
- Inspect the `.props` files in `Mdk.PbPackager` / `Mdk.ModPackager`.
- Capture the exact MSBuild invocation: `dotnet build <project> -v:detailed` and grep for `mdk.exe`.
- Reproduce manually with those exact parameters.

## Best practices

1. **Compare NuGet package vs. current source first.** Do not debug already-fixed bugs.
2. **Test with a working project first** to establish baseline behavior.
3. **Always pass the `.csproj` path explicitly** — never trust directory auto-detection.
4. **Use trace mode liberally** when the version supports it.
5. **Keep timeouts short** (15–30s) to detect hangs fast.
6. **Stop runaway processes immediately** — `Get-Process mdk | Stop-Process -Force`.
7. **Document findings in `.issues/`** — one markdown file per issue.
8. **Diff working vs. failing** to localize the divergence.

## Useful PowerShell snippets

```powershell
# Build MDK
dotnet build Source\Mdk.CommandLine\Mdk.CommandLine.csproj -c Debug

# Run with capture, take first 20 lines
$output = & "Source\Mdk.CommandLine\bin\Debug\net9.0\win-x64\mdk.exe" pack "project.csproj" 2>&1 | Select-Object -First 20

# Find all MDK test projects
Get-ChildItem -Recurse -Filter "mdk.ini" | Select-Object Directory

# Kill hung MDK process
Get-Process mdk | Stop-Process -Force

# Launch + check whether it exited within 5s
$proc = Start-Process "Source\Mdk.CommandLine\bin\Debug\net9.0\win-x64\mdk.exe" -ArgumentList "pack","project.csproj" -PassThru -NoNewWindow
Start-Sleep -Seconds 5
$proc.HasExited
```

## Creating a minimal test project

1. Create a new directory for the test.
2. Copy a working `.csproj` from `Source\Mdk.CommandLine.Tests\TestData\`.
3. Add a minimal `Program.cs`.
4. Add `mdk.ini` and `mdk.local.ini`.
5. Run `mdk restore` to set up references.
6. Run `mdk pack` to verify.

## Documenting issues

When you find a bug or open an investigation, create `.issues/<descriptive-name>.md` containing:

- **Minimal reproduction steps** — the simplest trigger.
- **Symptoms** — what actually happens.
- **Expected behavior** — what should happen.
- **Status** — investigation progress, fixes needed.
- **NuGet package vs. current source** — results from Step 0 above.

The `.issues/` folder is gitignored — use it freely for:
- Investigation notes
- Captured trace logs and build output
- Diagnostic dumps
- Throwaway test projects / minimal repros

```bash
# Capture detailed MSBuild output for analysis
dotnet build project.csproj -v:detailed > .issues\build-output.txt 2>&1

# Save trace logs
& mdk.exe pack project.csproj --trace > .issues\trace-log.txt 2>&1

# Stash a minimal repro project
mkdir .issues\test-case
```

## Code map (where to look)

- **`Source\Mdk.CommandLine\Program.cs`** — entry point, command routing, exception handling
- **`Source\Mdk.CommandLine\Parameters.cs`** — CLI argument parsing
- **`Source\Mdk.CommandLine\MdkProject.cs`** — project type detection / loading
- **`Source\Mdk.CommandLine\IngameScript\Pack\`** — PB script packaging logic
- **`Source\Mdk.CommandLine\Mod\Pack\`** — mod packaging logic
- **`Source\Mdk.CommandLine\DirectConsole.cs`** — console output implementation
