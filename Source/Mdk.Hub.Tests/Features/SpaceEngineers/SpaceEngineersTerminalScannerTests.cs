using System.Collections.Generic;
using System.IO;
using Mdk.Hub.Features.SpaceEngineers;

namespace Mdk.Hub.Tests.Features.SpaceEngineers;

/// <summary>
///     Unit tests for <see cref="SpaceEngineersTerminalScanner" /> using fake DLLs built with
///     <see cref="FakeGameDllBuilder" />.  No real Space Engineers installation required.
/// </summary>
[TestFixture]
public class SpaceEngineersTerminalScannerTests
{
    string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"mdk-scanner-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Test]
    public void Scan_TypeWithBothAttributes_IncludesTypeId()
    {
        FakeGameDllBuilder.Create(_tempDir, ("TestRefinery", true));

        var result = SpaceEngineersTerminalScanner.Scan(_tempDir);

        TestContext.Out.WriteLine($"Scanned TypeIds: [{string.Join(", ", result)}]");
        Assert.That(result, Contains.Item("TestRefinery"));
    }

    [Test]
    public void Scan_TypeWithOnlyCubeBlockTypeAttribute_ExcludesTypeId()
    {
        FakeGameDllBuilder.Create(_tempDir, ("TestArmor", false));

        var result = SpaceEngineersTerminalScanner.Scan(_tempDir);

        TestContext.Out.WriteLine($"Scanned TypeIds: [{string.Join(", ", result)}]");
        Assert.That(result, Does.Not.Contain("TestArmor"));
    }

    [Test]
    public void Scan_MixedTypes_OnlyTerminalTypesIncluded()
    {
        FakeGameDllBuilder.Create(_tempDir,
            ("TestRefinery", true),
            ("TestGyro", true),
            ("TestArmor", false),
            ("TestConveyor", false));

        var result = SpaceEngineersTerminalScanner.Scan(_tempDir);

        TestContext.Out.WriteLine($"Scanned TypeIds: [{string.Join(", ", result)}]");
        Assert.That(result, Contains.Item("TestRefinery"));
        Assert.That(result, Contains.Item("TestGyro"));
        Assert.That(result, Does.Not.Contain("TestArmor"));
        Assert.That(result, Does.Not.Contain("TestConveyor"));
    }

    [Test]
    public void Scan_StripsPrefixFromObjectBuilderTypeName()
    {
        // The DLL contains MyObjectBuilder_TestThrust → TypeId should be "TestThrust"
        FakeGameDllBuilder.Create(_tempDir, ("TestThrust", true));

        var result = SpaceEngineersTerminalScanner.Scan(_tempDir);

        TestContext.Out.WriteLine($"Scanned TypeIds: [{string.Join(", ", result)}]");
        Assert.That(result, Contains.Item("TestThrust"));
        Assert.That(result, Does.Not.Contain("MyObjectBuilder_TestThrust"));
    }

    [Test]
    public void Scan_NonexistentBinDir_ReturnsEmpty()
    {
        var result = SpaceEngineersTerminalScanner.Scan(Path.Combine(_tempDir, "doesnotexist"));

        TestContext.Out.WriteLine($"Scanned TypeIds: [{string.Join(", ", result)}]");
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Scan_EmptyBinDir_ReturnsEmpty()
    {
        // Directory exists but contains no DLLs matching expected names.
        var result = SpaceEngineersTerminalScanner.Scan(_tempDir);

        TestContext.Out.WriteLine($"Scanned TypeIds: [{string.Join(", ", result)}]");
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Scan_GetDllPaths_ReturnsTwoExpectedFilenames()
    {
        var paths = SpaceEngineersTerminalScanner.GetDllPaths(_tempDir);

        TestContext.Out.WriteLine("DLL paths:");
        foreach (var p in paths)
            TestContext.Out.WriteLine($"  {p}");

        Assert.That(paths, Has.Length.EqualTo(2));
        Assert.That(paths, Has.Some.EndsWith("Sandbox.Game.dll"));
        Assert.That(paths, Has.Some.EndsWith("SpaceEngineers.Game.dll"));
    }
}
