using System.Collections.Immutable;
using FakeItEasy;
using Mdk.CommandLine;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.Mod.Pack;
using Mdk.CommandLine.Mod.Pack.Jobs;
using Mdk.CommandLine.Shared;
using Mdk.CommandLine.Shared.Api;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.Mod;

[TestFixture]
public class DryRunPackTests
{
    string _root = null!;
    string _projectPath = null!;
    string _outputPath = null!;
    string _tracePath = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "mdk-dry-run-" + Guid.NewGuid().ToString("N"));
        _projectPath = Path.Combine(_root, "Project");
        _outputPath = Path.Combine(_root, "Output", "TestMod");
        _tracePath = Path.Combine(_root, "Trace");
        Directory.CreateDirectory(_projectPath);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    [Test]
    public async Task PrepareOutputJob_DryRun_LeavesExistingOutputUntouched()
    {
        await CreatePreviousOutputAsync();
        var before = SnapshotOutput();

        await new PrepareOutputJob().ExecuteAsync(CreateContext(isDryRun: true));

        Assert.That(SnapshotOutput(), Is.EqualTo(before));
    }

    [Test]
    public async Task PrepareOutputJob_DryRun_ReportsFoldersARealRunWouldDelete()
    {
        await CreatePreviousOutputAsync();
        var console = A.Fake<IConsole>();

        await new PrepareOutputJob().ExecuteAsync(CreateContext(isDryRun: true, console));

        A.CallTo(() => console.Trace($"Dry run: would delete {Path.Combine(".", "Data", "Scripts")}", A<int>._)).MustHaveHappened();
        A.CallTo(() => console.Trace($"Dry run: would delete {Path.Combine(".", "Data")}", A<int>._)).MustHaveHappened();
    }

    [Test]
    public async Task PrepareOutputJob_DryRun_DoesNotCreateMissingOutput()
    {
        await new PrepareOutputJob().ExecuteAsync(CreateContext(isDryRun: true));

        Assert.That(Directory.Exists(_outputPath), Is.False);
    }

    [Test]
    public async Task PrepareOutputJob_NotDryRun_DeletesStaleFiles()
    {
        await CreatePreviousOutputAsync();

        await new PrepareOutputJob().ExecuteAsync(CreateContext(isDryRun: false));

        Assert.That(File.Exists(Path.Combine(_outputPath, "stale.txt")), Is.False);
        Assert.That(Directory.Exists(Path.Combine(_outputPath, "Data")), Is.False);
        Assert.That(File.Exists(Path.Combine(_outputPath, "mdk.meta")), Is.True);
    }

    [Test]
    public async Task DryRunFileSystem_WritesNothingToOutput()
    {
        var sourceFile = Path.Combine(_projectPath, "thumb.png");
        await File.WriteAllTextAsync(sourceFile, "image");
        var fileSystem = new DryRunFileSystem(new PackFileSystem(_projectPath, _outputPath, _tracePath, A.Fake<IConsole>()), A.Fake<IConsole>());

        await fileSystem.WriteAsync("mdk.meta", "meta");
        await fileSystem.CopyAsync(sourceFile, Path.Combine(_outputPath, "thumb.png"), true);
        var wouldCreate = await fileSystem.CreateFolderAsync("Data");

        Assert.That(wouldCreate, Is.True);
        Assert.That(Directory.Exists(_outputPath), Is.False);
    }

    [Test]
    public async Task DryRunFileSystem_RemembersWhatItWouldHaveWritten()
    {
        var fileSystem = new DryRunFileSystem(new PackFileSystem(_projectPath, _outputPath, _tracePath, A.Fake<IConsole>()), A.Fake<IConsole>());

        await fileSystem.WriteAsync("mdk.meta", "meta");
        var firstCreate = await fileSystem.CreateFolderAsync("Data");
        var secondCreate = await fileSystem.CreateFolderAsync("Data");

        Assert.That(fileSystem.Exists("mdk.meta"), Is.True);
        Assert.That(firstCreate, Is.True);
        Assert.That(secondCreate, Is.False);
    }

    [Test]
    public async Task DryRunFileSystem_StillWritesTraceFiles()
    {
        var fileSystem = new DryRunFileSystem(new PackFileSystem(_projectPath, _outputPath, _tracePath, A.Fake<IConsole>()), A.Fake<IConsole>());

        await fileSystem.WriteTraceAsync("trace.txt", "trace");

        Assert.That(File.ReadAllText(Path.Combine(_tracePath, "trace.txt")), Is.EqualTo("trace"));
    }

    async Task CreatePreviousOutputAsync()
    {
        Directory.CreateDirectory(Path.Combine(_outputPath, "Data", "Scripts"));
        await MetaFile.WriteAsync(new PackFileSystem(_projectPath, _outputPath, _tracePath, A.Fake<IConsole>()), MdkProjectType.Mod);
        await File.WriteAllTextAsync(Path.Combine(_outputPath, "stale.txt"), "stale");
        await File.WriteAllTextAsync(Path.Combine(_outputPath, "Data", "Scripts", "Old.cs"), "class Old { }");
    }

    ModPackContext CreateContext(bool isDryRun, IConsole? console = null)
    {
        var parameters = new Parameters { Verb = Verb.Pack };
        parameters.PackVerb.DryRun = isDryRun;
        console ??= A.Fake<IConsole>();
        var packFileSystem = new PackFileSystem(_projectPath, _outputPath, _tracePath, console);
        IFileSystem fileSystem = isDryRun ? new DryRunFileSystem(packFileSystem, console) : packFileSystem;
        return new ModPackContext(
            parameters,
            console,
            A.Fake<IInteraction>(o => o.Strict()),
            new FileFilter([], _projectPath),
            new FileFilter([], _outputPath),
            fileSystem,
            ImmutableHashSet<string>.Empty,
            null,
            default,
            default,
            default);
    }

    string[] SnapshotOutput() =>
        Directory.EnumerateFileSystemEntries(_outputPath, "*", SearchOption.AllDirectories)
            .Order()
            .Select(path => File.Exists(path) ? $"{path}:{File.ReadAllText(path)}" : path)
            .ToArray();
}
