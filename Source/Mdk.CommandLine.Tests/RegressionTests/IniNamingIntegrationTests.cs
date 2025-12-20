using FakeItEasy;
using Mdk.CommandLine;
using Mdk.CommandLine.Shared.Api;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.RegressionTests;

[TestFixture]
public class IniNamingIntegrationTests
{
    [Test]
    public async Task LoadProject_WithNewNaming_IdentifiesProjectTypeAndLoadsConfiguration()
    {
        // Arrange
        var projectPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData/NewNamingTest/NewNamingTest.csproj");
        var console = A.Fake<IConsole>();

        // Act
        var projects = MdkProject.LoadAsync(projectPath, console);
        var projectList = await projects.ToListAsync();

        // Assert
        Assert.That(projectList, Has.Count.EqualTo(1), "Should load one project");
        Assert.That(projectList[0].Type, Is.EqualTo(MdkProjectType.ProgrammableBlock), 
            "Should identify as programmable block from mdk.ini (NEW naming)");
    }

    [Test]
    public async Task LoadProject_WithOldNaming_IdentifiesProjectTypeAndLoadsConfiguration()
    {
        // Arrange
        var projectPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData/OldNamingTest/OldNamingTest.csproj");
        var console = A.Fake<IConsole>();

        // Act
        var projects = MdkProject.LoadAsync(projectPath, console);
        var projectList = await projects.ToListAsync();

        // Assert
        Assert.That(projectList, Has.Count.EqualTo(1), "Should load one project");
        Assert.That(projectList[0].Type, Is.EqualTo(MdkProjectType.ProgrammableBlock), 
            "Should identify as programmable block from OldNamingTest.mdk.ini (OLD naming)");
    }

    [Test]
    public async Task LoadProject_WithBothNamingStyles_PrefersNewNaming()
    {
        // Arrange - Create a temp directory with BOTH naming styles
        var tempDir = Path.Combine(Path.GetTempPath(), "IniNamingPriorityTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a minimal valid csproj
            var projectPath = Path.Combine(tempDir, "PriorityTest.csproj");
            File.WriteAllText(projectPath, @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
  </PropertyGroup>
</Project>");

            // Create NEW style with type=programmableblock
            File.WriteAllText(Path.Combine(tempDir, "mdk.ini"), @"[mdk]
type=programmableblock
");

            // Create OLD style with type=mod (different!)
            File.WriteAllText(Path.Combine(tempDir, "PriorityTest.mdk.ini"), @"[mdk]
type=mod
");

            var console = A.Fake<IConsole>();

            // Act
            var projects = MdkProject.LoadAsync(projectPath, console);
            var projectList = await projects.ToListAsync();

            // Assert - Should use NEW naming (mdk.ini), so type should be ProgrammableBlock, not Mod
            Assert.That(projectList, Has.Count.EqualTo(1), "Should load one project");
            Assert.That(projectList[0].Type, Is.EqualTo(MdkProjectType.ProgrammableBlock), 
                "Should prefer mdk.ini (new naming) over PriorityTest.mdk.ini (old naming)");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void IniFileFinder_WithNewNaming_FindsCorrectFiles()
    {
        // Arrange
        var projectPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData/NewNamingTest/NewNamingTest.csproj");

        // Act
        var mainIni = Mdk.CommandLine.Utility.IniFileFinder.FindMainIni(projectPath);
        var localIni = Mdk.CommandLine.Utility.IniFileFinder.FindLocalIni(projectPath);

        // Assert
        Assert.That(mainIni, Is.Not.Null, "Should find main INI");
        Assert.That(Path.GetFileName(mainIni), Is.EqualTo("mdk.ini"), "Should find mdk.ini (new naming)");
        
        Assert.That(localIni, Is.Not.Null, "Should find local INI");
        Assert.That(Path.GetFileName(localIni), Is.EqualTo("mdk.local.ini"), "Should find mdk.local.ini (new naming)");
    }

    [Test]
    public void IniFileFinder_WithOldNaming_FindsCorrectFiles()
    {
        // Arrange
        var projectPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData/OldNamingTest/OldNamingTest.csproj");

        // Act
        var mainIni = Mdk.CommandLine.Utility.IniFileFinder.FindMainIni(projectPath);
        var localIni = Mdk.CommandLine.Utility.IniFileFinder.FindLocalIni(projectPath);

        // Assert
        Assert.That(mainIni, Is.Not.Null, "Should find main INI");
        Assert.That(Path.GetFileName(mainIni), Is.EqualTo("OldNamingTest.mdk.ini"), "Should find OldNamingTest.mdk.ini (old naming)");
        
        Assert.That(localIni, Is.Not.Null, "Should find local INI");
        Assert.That(Path.GetFileName(localIni), Is.EqualTo("OldNamingTest.mdk.local.ini"), "Should find OldNamingTest.mdk.local.ini (old naming)");
    }

    [Test]
    public async Task LoadModProject_WithNewNaming_IdentifiesProjectTypeAndLoadsConfiguration()
    {
        // Arrange
        var projectPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData/NewNamingModTest/NewNamingModTest.csproj");
        var console = A.Fake<IConsole>();

        // Act
        var projects = MdkProject.LoadAsync(projectPath, console);
        var projectList = await projects.ToListAsync();

        // Assert
        Assert.That(projectList, Has.Count.EqualTo(1), "Should load one mod project");
        Assert.That(projectList[0].Type, Is.EqualTo(MdkProjectType.Mod), 
            "Should identify as mod from mdk.ini (NEW naming)");
    }

    [Test]
    public async Task LoadModProject_WithOldNaming_IdentifiesProjectTypeAndLoadsConfiguration()
    {
        // Arrange
        var projectPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData/OldNamingModTest/OldNamingModTest.csproj");
        var console = A.Fake<IConsole>();

        // Act
        var projects = MdkProject.LoadAsync(projectPath, console);
        var projectList = await projects.ToListAsync();

        // Assert
        Assert.That(projectList, Has.Count.EqualTo(1), "Should load one mod project");
        Assert.That(projectList[0].Type, Is.EqualTo(MdkProjectType.Mod), 
            "Should identify as mod from OldNamingModTest.mdk.ini (OLD naming)");
    }

    [Test]
    public void IniFileFinder_WithModNewNaming_FindsCorrectFiles()
    {
        // Arrange
        var projectPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData/NewNamingModTest/NewNamingModTest.csproj");

        // Act
        var mainIni = Mdk.CommandLine.Utility.IniFileFinder.FindMainIni(projectPath);
        var localIni = Mdk.CommandLine.Utility.IniFileFinder.FindLocalIni(projectPath);

        // Assert
        Assert.That(mainIni, Is.Not.Null, "Should find main INI for mod");
        Assert.That(Path.GetFileName(mainIni), Is.EqualTo("mdk.ini"), "Should find mdk.ini (new naming) for mod");
        
        Assert.That(localIni, Is.Not.Null, "Should find local INI for mod");
        Assert.That(Path.GetFileName(localIni), Is.EqualTo("mdk.local.ini"), "Should find mdk.local.ini (new naming) for mod");
    }

    [Test]
    public void IniFileFinder_WithModOldNaming_FindsCorrectFiles()
    {
        // Arrange
        var projectPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData/OldNamingModTest/OldNamingModTest.csproj");

        // Act
        var mainIni = Mdk.CommandLine.Utility.IniFileFinder.FindMainIni(projectPath);
        var localIni = Mdk.CommandLine.Utility.IniFileFinder.FindLocalIni(projectPath);

        // Assert
        Assert.That(mainIni, Is.Not.Null, "Should find main INI for mod");
        Assert.That(Path.GetFileName(mainIni), Is.EqualTo("OldNamingModTest.mdk.ini"), "Should find OldNamingModTest.mdk.ini (old naming) for mod");
        
        Assert.That(localIni, Is.Not.Null, "Should find local INI for mod");
        Assert.That(Path.GetFileName(localIni), Is.EqualTo("OldNamingModTest.mdk.local.ini"), "Should find OldNamingModTest.mdk.local.ini (old naming) for mod");
    }
}
