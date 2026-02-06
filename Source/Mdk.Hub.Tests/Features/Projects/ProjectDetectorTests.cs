using System;
using System.IO;
using Mdk.Hub.Features.Projects;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Utility;
using NUnit.Framework;

namespace Mdk.Hub.Tests.Features.Projects;

[TestFixture]
public class ProjectDetectorTests
{
    string _testDirectory = null!;

    [SetUp]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"MdkHubTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);
    }

    [Test]
    public void TryDetectProject_ValidProject_ReturnsTrue()
    {
        // Arrange - Create a valid MDK project with mdk.ini
        var projectPath = Path.Combine(_testDirectory, "TestProject.csproj");
        var iniPath = Path.Combine(_testDirectory, "mdk.ini");

        File.WriteAllText(projectPath, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Mal.Mdk2.PbPackager" Version="2.0.0" />
              </ItemGroup>
            </Project>
            """);

        File.WriteAllText(iniPath, """
            [mdk]
            type=programmableblock
            """);

        // Act
        var result = ProjectDetector.TryDetectProject(projectPath, out var projectInfo);

        // Assert
        Assert.That(result, Is.True, "Should detect valid MDK project");
        Assert.That(projectInfo, Is.Not.Null);
        Assert.That(projectInfo!.Name, Is.EqualTo("TestProject"));
        Assert.That(projectInfo.Type, Is.EqualTo(ProjectType.ProgrammableBlock));
        Assert.That(projectInfo.ProjectPath.Value, Is.EqualTo(new CanonicalPath(projectPath).Value));
    }

    [Test]
    public void TryDetectProject_WithMdkIni_DetectsCorrectly()
    {
        // Arrange - Project with new-style mdk.ini
        var projectPath = Path.Combine(_testDirectory, "NewStyle.csproj");
        var iniPath = Path.Combine(_testDirectory, "mdk.ini");

        File.WriteAllText(projectPath, """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Mal.Mdk2.PbPackager" Version="2.0.0" />
              </ItemGroup>
            </Project>
            """);

        File.WriteAllText(iniPath, "[mdk]\ntype=programmableblock");

        // Act
        var result = ProjectDetector.TryDetectProject(projectPath, out var projectInfo);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(projectInfo!.Type, Is.EqualTo(ProjectType.ProgrammableBlock));
    }

    [Test]
    public void TryDetectProject_WithMdkLocalIni_DetectsCorrectly()
    {
        // Arrange - Project with only mdk.local.ini (local-only project)
        var projectPath = Path.Combine(_testDirectory, "LocalOnly.csproj");
        var localIniPath = Path.Combine(_testDirectory, "mdk.local.ini");

        File.WriteAllText(projectPath, """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Mal.Mdk2.ModPackager" Version="2.0.0" />
              </ItemGroup>
            </Project>
            """);

        File.WriteAllText(localIniPath, "[mdk]\ntype=mod");

        // Act
        var result = ProjectDetector.TryDetectProject(projectPath, out var projectInfo);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(projectInfo!.Type, Is.EqualTo(ProjectType.Mod));
    }

    [Test]
    public void TryDetectProject_WithBothIniFiles_DetectsCorrectly()
    {
        // Arrange - Project with both mdk.ini and mdk.local.ini
        var projectPath = Path.Combine(_testDirectory, "BothInis.csproj");
        var mainIniPath = Path.Combine(_testDirectory, "mdk.ini");
        var localIniPath = Path.Combine(_testDirectory, "mdk.local.ini");

        File.WriteAllText(projectPath, """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Mal.Mdk2.PbPackager" Version="2.0.0" />
              </ItemGroup>
            </Project>
            """);

        File.WriteAllText(mainIniPath, "[mdk]\ntype=programmableblock");
        File.WriteAllText(localIniPath, "[mdk]\ninteractive=OpenHub");

        // Act
        var result = ProjectDetector.TryDetectProject(projectPath, out var projectInfo);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(projectInfo, Is.Not.Null);
    }

    [Test]
    public void TryDetectProject_MissingCsproj_ReturnsFalse()
    {
        // Arrange - Path to non-existent .csproj
        var projectPath = Path.Combine(_testDirectory, "NonExistent.csproj");

        // Act
        var result = ProjectDetector.TryDetectProject(projectPath, out var projectInfo);

        // Assert
        Assert.That(result, Is.False, "Should return false for missing .csproj");
        Assert.That(projectInfo, Is.Null);
    }

    [Test]
    public void TryDetectProject_NotMdkProject_ReturnsFalse()
    {
        // Arrange - Valid .csproj but no mdk.ini files
        var projectPath = Path.Combine(_testDirectory, "RegularProject.csproj");

        File.WriteAllText(projectPath, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        // Act
        var result = ProjectDetector.TryDetectProject(projectPath, out var projectInfo);

        // Assert
        Assert.That(result, Is.False, "Should return false for non-MDK project");
        Assert.That(projectInfo, Is.Null);
    }

    [Test]
    public void TryDetectProject_CorruptXml_HandleGracefully()
    {
        // Arrange - .csproj with invalid XML
        var projectPath = Path.Combine(_testDirectory, "Corrupt.csproj");
        var iniPath = Path.Combine(_testDirectory, "mdk.ini");

        File.WriteAllText(projectPath, "<Project><<Invalid XML");
        File.WriteAllText(iniPath, "[mdk]\ntype=programmableblock");

        // Act
        var result = ProjectDetector.TryDetectProject(projectPath, out var projectInfo);

        // Assert
        Assert.That(result, Is.False, "Should handle corrupt XML gracefully");
        Assert.That(projectInfo, Is.Null);
    }

    [Test]
    public void TryDetectProject_InvalidPath_ReturnsFalse()
    {
        // Arrange
        var invalidPaths = new[]
        {
            "",
            "   ",
            "not_a_csproj.txt",
            Path.Combine(_testDirectory, "NoExtension")
        };

        foreach (var invalidPath in invalidPaths)
        {
            // Act
            var result = ProjectDetector.TryDetectProject(invalidPath, out var projectInfo);

            // Assert
            Assert.That(result, Is.False, $"Should return false for invalid path: {invalidPath}");
            Assert.That(projectInfo, Is.Null);
        }
    }

    [Test]
    public void TryDetectProject_ModProject_DetectsModType()
    {
        // Arrange - Project with ModPackager reference
        var projectPath = Path.Combine(_testDirectory, "ModProject.csproj");
        var iniPath = Path.Combine(_testDirectory, "mdk.ini");

        File.WriteAllText(projectPath, """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Mal.Mdk2.ModPackager" Version="2.0.0" />
              </ItemGroup>
            </Project>
            """);

        File.WriteAllText(iniPath, "[mdk]\ntype=mod");

        // Act
        var result = ProjectDetector.TryDetectProject(projectPath, out var projectInfo);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(projectInfo!.Type, Is.EqualTo(ProjectType.Mod));
    }

    [Test]
    public void TryDetectProject_DefaultsToProgrammableBlock_WhenPackageAmbiguous()
    {
        // Arrange - Project without specific packager reference
        var projectPath = Path.Combine(_testDirectory, "Ambiguous.csproj");
        var iniPath = Path.Combine(_testDirectory, "mdk.ini");

        File.WriteAllText(projectPath, """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="SomeOtherPackage" Version="2.0.0" />
              </ItemGroup>
            </Project>
            """);

        File.WriteAllText(iniPath, "[mdk]");

        // Act
        var result = ProjectDetector.TryDetectProject(projectPath, out var projectInfo);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(projectInfo!.Type, Is.EqualTo(ProjectType.ProgrammableBlock), "Should default to ProgrammableBlock");
    }
}
