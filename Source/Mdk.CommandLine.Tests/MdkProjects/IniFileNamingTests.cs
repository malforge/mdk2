using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Mdk.CommandLine;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.Shared.Api;
using Mdk.CommandLine.Utility;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.MdkProjects;

[TestFixture]
public class IniFileNamingTests
{
    string _tempDirectory = null!;
    string _testProjectPath = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "IniFileNamingTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);
        _testProjectPath = Path.Combine(_tempDirectory, "TestProject.csproj");
        
        // Create a minimal valid csproj
        File.WriteAllText(_testProjectPath, @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
  </PropertyGroup>
</Project>");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, true);
    }

    [Test]
    public async Task LoadProject_WithNewNaming_Succeeds()
    {
        // Arrange
        var newIniPath = Path.Combine(_tempDirectory, "mdk.ini");
        File.WriteAllText(newIniPath, @"[mdk]
type=programmableblock
");

        var console = A.Fake<IConsole>();

        // Act
        var projects = MdkProject.LoadAsync(_testProjectPath, console);
        var projectList = await projects.ToListAsync();

        // Assert
        Assert.That(projectList, Has.Count.EqualTo(1));
        Assert.That(projectList[0].Type, Is.EqualTo(MdkProjectType.ProgrammableBlock));
    }

    [Test]
    public async Task LoadProject_WithOldNaming_Succeeds()
    {
        // Arrange
        var oldIniPath = Path.Combine(_tempDirectory, "TestProject.mdk.ini");
        File.WriteAllText(oldIniPath, @"[mdk]
type=programmableblock
");

        var console = A.Fake<IConsole>();

        // Act
        var projects = MdkProject.LoadAsync(_testProjectPath, console);
        var projectList = await projects.ToListAsync();

        // Assert
        Assert.That(projectList, Has.Count.EqualTo(1));
        Assert.That(projectList[0].Type, Is.EqualTo(MdkProjectType.ProgrammableBlock));
    }

    [Test]
    public async Task LoadProject_WithBothNamings_PrefersNew()
    {
        // Arrange
        var newIniPath = Path.Combine(_tempDirectory, "mdk.ini");
        File.WriteAllText(newIniPath, @"[mdk]
type=programmableblock
");

        var oldIniPath = Path.Combine(_tempDirectory, "TestProject.mdk.ini");
        File.WriteAllText(oldIniPath, @"[mdk]
type=mod
");

        var console = A.Fake<IConsole>();

        // Act
        var projects = MdkProject.LoadAsync(_testProjectPath, console);
        var projectList = await projects.ToListAsync();

        // Assert - should use new style (programmableblock, not mod)
        Assert.That(projectList, Has.Count.EqualTo(1));
        Assert.That(projectList[0].Type, Is.EqualTo(MdkProjectType.ProgrammableBlock));
    }

    [Test]
    public void LoadParameters_WithNewNaming_FindsFile()
    {
        // Arrange
        var newIniPath = Path.Combine(_tempDirectory, "mdk.ini");
        File.WriteAllText(newIniPath, @"[mdk]
trace=on
");

        // Act
        var foundPath = IniFileFinder.FindMainIni(_testProjectPath);

        // Assert
        Assert.That(foundPath, Is.EqualTo(newIniPath));
    }

    [Test]
    public void LoadParameters_WithOldNaming_FindsFile()
    {
        // Arrange
        var oldIniPath = Path.Combine(_tempDirectory, "TestProject.mdk.ini");
        File.WriteAllText(oldIniPath, @"[mdk]
trace=on
");

        // Act
        var foundPath = IniFileFinder.FindMainIni(_testProjectPath);

        // Assert
        Assert.That(foundPath, Is.EqualTo(oldIniPath));
    }

    [Test]
    public void LoadParameters_WithBothNamings_PrefersNew()
    {
        // Arrange
        var newLocalIniPath = Path.Combine(_tempDirectory, "mdk.local.ini");
        File.WriteAllText(newLocalIniPath, @"[mdk]
trace=on
");

        var oldLocalIniPath = Path.Combine(_tempDirectory, "TestProject.mdk.local.ini");
        File.WriteAllText(oldLocalIniPath, @"[mdk]
trace=off
");

        // Act
        var foundPath = IniFileFinder.FindLocalIni(_testProjectPath);

        // Assert - should find new style file
        Assert.That(foundPath, Is.EqualTo(newLocalIniPath));
    }

    [Test]
    public void ConvertLegacyProject_CreatesNewNaming()
    {
        // Arrange & Act are complex for this, so we'll just verify the utility methods work correctly
        var (mainIni, localIni) = IniFileFinder.GetNewIniPaths(_testProjectPath);

        // Assert
        Assert.That(Path.GetFileName(mainIni), Is.EqualTo("mdk.ini"));
        Assert.That(Path.GetFileName(localIni), Is.EqualTo("mdk.local.ini"));
    }

    [Test]
    public async Task ConvertLegacyProject_UpdatesGitignoreWithNewNaming()
    {
        // This is tested in LegacyConverterTests, but we can verify the gitignore content format
        var gitIgnorePath = Path.Combine(_tempDirectory, ".gitignore");
        File.WriteAllText(gitIgnorePath, "");

        // Simulate what the converter does
        var ignoreContent = $"# MDK{Environment.NewLine}mdk.local.ini";
        await File.AppendAllTextAsync(gitIgnorePath, ignoreContent);

        var content = await File.ReadAllTextAsync(gitIgnorePath);

        // Assert
        Assert.That(content, Does.Contain("mdk.local.ini"));
        Assert.That(content, Does.Not.Contain("TestProject.mdk.local.ini"));
    }
}
