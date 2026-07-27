# Debugging MDK

How to reproduce and diagnose issues in MDK itself - the CLI, the packagers and the
analyzers. For issues in your own scripts, see the
[user documentation](https://malforge.github.io/spaceengineers/mdk2/) instead.

## Prerequisites

- This repository checked out
- .NET 9.0 SDK
- A project to test against, ideally both a working one and a failing one

## Building the CLI

From the `Source` directory:

```bash
dotnet build Mdk.CommandLine/Mdk.CommandLine.csproj -c Debug
```

The executable lands in `Mdk.CommandLine/bin/Debug/net9.0/win-x64/mdk.exe`.

## Running it against a project

```bash
Mdk.CommandLine/bin/Debug/net9.0/win-x64/mdk.exe pack "path/to/project.csproj"
Mdk.CommandLine/bin/Debug/net9.0/win-x64/mdk.exe restore "path/to/project.csproj"
```

Always pass the full path to the `.csproj` file, not the directory containing it.

Add `-trace` for verbose output showing the execution flow. Note the single dash - the
argument is matched exactly, so `--trace` is silently ignored.

```bash
Mdk.CommandLine/bin/Debug/net9.0/win-x64/mdk.exe pack "path/to/project.csproj" -trace
```

## Test projects

Ready-made projects live in `Source/Mdk.CommandLine.Tests/TestData/`. `NewNamingTest` is a
simple script project and `NewNamingModTest` a simple mod; most of the rest are named after
the issue they were added for.

The templates under `Source/ScriptTemplates/content/` are not always configured for
debugging, so prefer the test data projects.

## Check the released package first

Before digging in, find out whether the bug still exists in current source. Run the same
command through both the released package and your build:

```bash
# the released version, from your NuGet cache
~/.nuget/packages/mal.mdk2.pbpackager/<version>/tools/win-x64/mdk.exe pack "project.csproj"

# your build
Mdk.CommandLine/bin/Debug/net9.0/win-x64/mdk.exe pack "project.csproj"
```

- Fails in the package, works in source: already fixed, needs a release
- Fails in both: still needs fixing
- Works in the package, fails in source: a regression, worth bisecting
- Works in both: the reproduction steps aren't right yet

## Reproducing behaviour seen through MSBuild

MDK runs differently under MSBuild than it does by hand. To match what MSBuild does, pass
the same arguments it does, including `-configuration` and `-interactive`:

```bash
mdk.exe pack "project.csproj" -configuration Debug -interactive DoNothing
```

If a pack appears to hang, run it with a short timeout so you notice quickly, and capture
the output to a file rather than watching the console.

## Running the tests

```bash
cd Source
dotnet test MDK-Complete.slnx
```

We use NUnit.

**On Linux this will not work.** `MDK-Complete.slnx` includes Mdk.Hub, which multi-targets
`net9.0-windows` and cannot build there. You can still run individual test projects:

```bash
dotnet test Mdk.CommandLine.Tests/Mdk.CommandLine.Tests.csproj
```

Some regression tests pack real projects, which needs Space Engineers installed. Those
self-skip when the game is absent rather than failing.

CI runs the full suite on Windows for every pull request, so tests you add will be verified
there even if you cannot run them locally.
