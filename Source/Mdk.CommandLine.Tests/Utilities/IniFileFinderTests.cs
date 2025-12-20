using System;
using System.IO;
using Mdk.CommandLine.Utility;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.Utilities;

[TestFixture]
public class IniFileFinderTests
{
    string _tempDirectory = null!;
    string _testProjectPath = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "IniFileFinderTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);
        _testProjectPath = Path.Combine(_tempDirectory, "TestProject.csproj");
        File.WriteAllText(_testProjectPath, "<Project />");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, true);
    }

    [Test]
    public void FindMainIni_WhenNewStyleExists_ReturnsNewStyle()
    {
        // Arrange
        var newStylePath = Path.Combine(_tempDirectory, "mdk.ini");
        File.WriteAllText(newStylePath, "[mdk]");

        // Act
        var result = IniFileFinder.FindMainIni(_testProjectPath);

        // Assert
        Assert.That(result, Is.EqualTo(newStylePath));
    }

    [Test]
    public void FindMainIni_WhenOnlyOldStyleExists_ReturnsOldStyle()
    {
        // Arrange
        var oldStylePath = Path.Combine(_tempDirectory, "TestProject.mdk.ini");
        File.WriteAllText(oldStylePath, "[mdk]");

        // Act
        var result = IniFileFinder.FindMainIni(_testProjectPath);

        // Assert
        Assert.That(result, Is.EqualTo(oldStylePath));
    }

    [Test]
    public void FindMainIni_WhenBothExist_ReturnsNewStyle()
    {
        // Arrange
        var newStylePath = Path.Combine(_tempDirectory, "mdk.ini");
        var oldStylePath = Path.Combine(_tempDirectory, "TestProject.mdk.ini");
        File.WriteAllText(newStylePath, "[mdk] type=new");
        File.WriteAllText(oldStylePath, "[mdk] type=old");

        // Act
        var result = IniFileFinder.FindMainIni(_testProjectPath);

        // Assert
        Assert.That(result, Is.EqualTo(newStylePath));
    }

    [Test]
    public void FindMainIni_WhenNeitherExists_ReturnsNull()
    {
        // Act
        var result = IniFileFinder.FindMainIni(_testProjectPath);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindLocalIni_WhenNewStyleExists_ReturnsNewStyle()
    {
        // Arrange
        var newStylePath = Path.Combine(_tempDirectory, "mdk.local.ini");
        File.WriteAllText(newStylePath, "[mdk]");

        // Act
        var result = IniFileFinder.FindLocalIni(_testProjectPath);

        // Assert
        Assert.That(result, Is.EqualTo(newStylePath));
    }

    [Test]
    public void FindLocalIni_WhenOnlyOldStyleExists_ReturnsOldStyle()
    {
        // Arrange
        var oldStylePath = Path.Combine(_tempDirectory, "TestProject.mdk.local.ini");
        File.WriteAllText(oldStylePath, "[mdk]");

        // Act
        var result = IniFileFinder.FindLocalIni(_testProjectPath);

        // Assert
        Assert.That(result, Is.EqualTo(oldStylePath));
    }

    [Test]
    public void FindLocalIni_WhenBothExist_ReturnsNewStyle()
    {
        // Arrange
        var newStylePath = Path.Combine(_tempDirectory, "mdk.local.ini");
        var oldStylePath = Path.Combine(_tempDirectory, "TestProject.mdk.local.ini");
        File.WriteAllText(newStylePath, "[mdk] type=new");
        File.WriteAllText(oldStylePath, "[mdk] type=old");

        // Act
        var result = IniFileFinder.FindLocalIni(_testProjectPath);

        // Assert
        Assert.That(result, Is.EqualTo(newStylePath));
    }

    [Test]
    public void FindLocalIni_WhenNeitherExists_ReturnsNull()
    {
        // Act
        var result = IniFileFinder.FindLocalIni(_testProjectPath);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetNewIniPaths_ReturnsCorrectPaths()
    {
        // Act
        var (mainIni, localIni) = IniFileFinder.GetNewIniPaths(_testProjectPath);

        // Assert
        Assert.That(mainIni, Is.EqualTo(Path.Combine(_tempDirectory, "mdk.ini")));
        Assert.That(localIni, Is.EqualTo(Path.Combine(_tempDirectory, "mdk.local.ini")));
    }

    [Test]
    public void GetLegacyIniPaths_ReturnsCorrectPaths()
    {
        // Act
        var (mainIni, localIni) = IniFileFinder.GetLegacyIniPaths(_testProjectPath);

        // Assert
        Assert.That(mainIni, Is.EqualTo(Path.Combine(_tempDirectory, "TestProject.mdk.ini")));
        Assert.That(localIni, Is.EqualTo(Path.Combine(_tempDirectory, "TestProject.mdk.local.ini")));
    }

    [Test]
    public void FindMainIni_WithNullPath_ReturnsNull()
    {
        // Act
        var result = IniFileFinder.FindMainIni(null!);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindLocalIni_WithNullPath_ReturnsNull()
    {
        // Act
        var result = IniFileFinder.FindLocalIni(null!);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetNewIniPaths_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => IniFileFinder.GetNewIniPaths(null!));
    }

    [Test]
    public void GetLegacyIniPaths_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => IniFileFinder.GetLegacyIniPaths(null!));
    }
}
