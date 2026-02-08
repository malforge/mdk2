using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FakeItEasy;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Projects;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Storage;
using Mdk.Hub.Utility;
using NUnit.Framework;

namespace Mdk.Hub.Tests.Features.Projects;

[TestFixture]
public class ProjectRegistryTests
{
    ILogger _logger = null!;
    InMemoryFileStorageService _fileStorage = null!;
    ProjectRegistry _registry = null!;

    [SetUp]
    public void Setup()
    {
        _logger = A.Fake<ILogger>();
        _fileStorage = new InMemoryFileStorageService();
        _registry = new ProjectRegistry(_logger, _fileStorage);
    }

    [Test]
    public void GetProjects_EmptyRegistry_ReturnsEmptyList()
    {
        // Act
        var projects = _registry.GetProjects();

        // Assert
        Assert.That(projects, Is.Empty);
    }

    [Test]
    public void AddOrUpdateProject_NewProject_AddsSuccessfully()
    {
        // Arrange
        var project = CreateTestProject("TestProject", @"C:\Projects\Test\Test.csproj");

        // Act
        _registry.AddOrUpdateProject(project);
        var projects = _registry.GetProjects();

        // Assert
        Assert.That(projects, Has.Count.EqualTo(1));
        Assert.That(projects[0].Name, Is.EqualTo("TestProject"));
        Assert.That(projects[0].ProjectPath.Value, Is.EqualTo(@"C:\Projects\Test\Test.csproj"));
    }

    [Test]
    public void AddOrUpdateProject_DuplicatePath_UpdatesExisting()
    {
        // Arrange
        var project1 = CreateTestProject("OriginalName", @"C:\Projects\Test\Test.csproj");
        var project2 = CreateTestProject("UpdatedName", @"C:\Projects\Test\Test.csproj");

        // Act
        _registry.AddOrUpdateProject(project1);
        _registry.AddOrUpdateProject(project2);
        var projects = _registry.GetProjects();

        // Assert
        Assert.That(projects, Has.Count.EqualTo(1), "Should only have one project, not duplicates");
        Assert.That(projects[0].Name, Is.EqualTo("UpdatedName"), "Should have updated name");
    }

    [Test]
    public void RemoveProject_ExistingProject_RemovesSuccessfully()
    {
        // Arrange
        var project = CreateTestProject("TestProject", @"C:\Projects\Test\Test.csproj");
        _registry.AddOrUpdateProject(project);

        // Act
        _registry.RemoveProject(@"C:\Projects\Test\Test.csproj");
        var projects = _registry.GetProjects();

        // Assert
        Assert.That(projects, Is.Empty);
    }

    [Test]
    public void RemoveProject_NonExistentProject_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => _registry.RemoveProject(@"C:\NonExistent\Project.csproj"));
    }

    [Test]
    public void LoadFromFile_ValidJson_LoadsProjects()
    {
        // Arrange
        var json = """
        [
          {
            "Name": "Project1",
            "ProjectPath": "C:\\Projects\\Project1\\Project1.csproj",
            "Type": 0,
            "LastReferenced": "2026-01-01T12:00:00Z"
          },
          {
            "Name": "Project2",
            "ProjectPath": "C:\\Projects\\Project2\\Project2.csproj",
            "Type": 1,
            "LastReferenced": "2026-01-02T12:00:00Z",
            "NeedsUpdate": true,
            "UpdateCount": 2
          }
        ]
        """;
        var registryPath = _fileStorage.GetApplicationDataPath("projects.json");
        _fileStorage.WriteAllText(registryPath, json);

        // Act
        var registry = new ProjectRegistry(_logger, _fileStorage);
        var projects = registry.GetProjects();

        // Assert
        Assert.That(projects, Has.Count.EqualTo(2));
        Assert.That(projects[0].Name, Is.EqualTo("Project2"), "Should be ordered by LastReferenced descending");
        Assert.That(projects[1].Name, Is.EqualTo("Project1"));
        Assert.That(projects[0].Type, Is.EqualTo(ProjectType.Mod));
        Assert.That(projects[0].NeedsUpdate, Is.True);
        Assert.That(projects[0].UpdateCount, Is.EqualTo(2));
    }

    [Test]
    public void LoadFromFile_MissingFile_CreatesEmpty()
    {
        // Act - constructor loads
        var projects = _registry.GetProjects();

        // Assert
        Assert.That(projects, Is.Empty);
        A.CallTo(() => _logger.Info(A<string>.That.Contains("No existing registry found"), A<string>._, A<int>._, A<string>._)).MustHaveHappened();
    }

    [Test]
    public void LoadFromFile_CorruptJson_HandlesGracefully()
    {
        // Arrange
        var registryPath = _fileStorage.GetApplicationDataPath("projects.json");
        _fileStorage.WriteAllText(registryPath, "{ this is not valid json [[]");

        // Act
        var registry = new ProjectRegistry(_logger, _fileStorage);
        var projects = registry.GetProjects();

        // Assert
        Assert.That(projects, Is.Empty, "Should return empty list on corrupt JSON");
        A.CallTo(() => _logger.Error(A<string>.That.Contains("Failed to parse"), A<Exception>._, A<string>._, A<int>._, A<string>._)).MustHaveHappened();
    }

    [Test]
    public void SaveToFile_WithProjects_PersistsCorrectly()
    {
        // Arrange
        var project1 = CreateTestProject("Project1", @"C:\Projects\Project1\Project1.csproj");
        var project2 = CreateTestProject("Project2", @"C:\Projects\Project2\Project2.csproj");
        _registry.AddOrUpdateProject(project1);
        _registry.AddOrUpdateProject(project2);

        // Act
        var newRegistry = new ProjectRegistry(_logger, _fileStorage);
        var projects = newRegistry.GetProjects();

        // Assert
        Assert.That(projects, Has.Count.EqualTo(2));
        Assert.That(projects.Select(p => p.Name), Does.Contain("Project1"));
        Assert.That(projects.Select(p => p.Name), Does.Contain("Project2"));
    }

    [Test]
    public void AddOrUpdateProject_UpdatesLastReferenced()
    {
        // Arrange
        var oldTime = DateTimeOffset.Now.AddDays(-1);
        var project = CreateTestProject("TestProject", @"C:\Projects\Test\Test.csproj") with { LastReferenced = oldTime };

        // Act
        _registry.AddOrUpdateProject(project);
        var projects = _registry.GetProjects();

        // Assert
        Assert.That(projects[0].LastReferenced, Is.GreaterThan(oldTime), "LastReferenced should be updated to current time");
    }

    [Test]
    public void SaveToFile_SimulatedProjects_ExcludedFromPersistence()
    {
        // Arrange
        var realProject = CreateTestProject("RealProject", @"C:\Projects\Real\Real.csproj");
        var simulatedProject = CreateTestProject("SimulatedProject", @"C:\Projects\Simulated\Simulated.csproj") 
            with { Flags = ProjectFlags.Simulated };
        
        _registry.AddOrUpdateProject(realProject);
        _registry.AddOrUpdateProject(simulatedProject);

        // Act - reload from storage
        var newRegistry = new ProjectRegistry(_logger, _fileStorage);
        var projects = newRegistry.GetProjects();

        // Assert
        Assert.That(projects, Has.Count.EqualTo(1), "Simulated projects should not be persisted");
        Assert.That(projects[0].Name, Is.EqualTo("RealProject"));
    }

    [Test]
    public void GetProjects_OrdersByLastReferencedDescending()
    {
        // Arrange
        var oldProject = CreateTestProject("Old", @"C:\Projects\Old\Old.csproj") with { LastReferenced = DateTimeOffset.Now.AddDays(-10) };
        var recentProject = CreateTestProject("Recent", @"C:\Projects\Recent\Recent.csproj") with { LastReferenced = DateTimeOffset.Now.AddDays(-1) };
        var newestProject = CreateTestProject("Newest", @"C:\Projects\Newest\Newest.csproj") with { LastReferenced = DateTimeOffset.Now };

        // Act
        _registry.AddOrUpdateProject(oldProject);
        _registry.AddOrUpdateProject(recentProject);
        _registry.AddOrUpdateProject(newestProject);
        var projects = _registry.GetProjects();

        // Assert
        Assert.That(projects[0].Name, Is.EqualTo("Newest"));
        Assert.That(projects[1].Name, Is.EqualTo("Recent"));
        Assert.That(projects[2].Name, Is.EqualTo("Old"));
    }

    ProjectInfo CreateTestProject(string name, string path)
    {
        return new ProjectInfo
        {
            Name = name,
            ProjectPath = new CanonicalPath(path),
            Type = ProjectType.ProgrammableBlock,
            LastReferenced = DateTimeOffset.Now.AddDays(-1)
        };
    }
}
