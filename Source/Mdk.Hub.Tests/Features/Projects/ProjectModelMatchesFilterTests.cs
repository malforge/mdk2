using System;
using FakeItEasy;
using Mdk.Hub.Features.Projects;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Utility;
using NUnit.Framework;

namespace Mdk.Hub.Tests.Features.Projects;

[TestFixture]
public class ProjectModelMatchesFilterTests
{
    static ProjectModel CreateProject(ProjectType type, string name = "Test")
    {
        var shell = A.Fake<IShell>();
        return new ProjectModel(type, name, new CanonicalPath(@"C:\Projects\Test\Test.csproj"), DateTimeOffset.Now, shell);
    }

    [Test]
    public void MatchesFilter_MustBeMod_ExcludesProgrammableBlockProjects()
    {
        var script = CreateProject(ProjectType.ProgrammableBlock);

        var matches = script.MatchesFilter(string.Empty, mustBeMod: true, mustBeScript: false);

        Assert.That(matches, Is.False);
    }

    [Test]
    public void MatchesFilter_MustBeMod_IncludesModProjects()
    {
        var mod = CreateProject(ProjectType.Mod);

        var matches = mod.MatchesFilter(string.Empty, mustBeMod: true, mustBeScript: false);

        Assert.That(matches, Is.True);
    }

    [Test]
    public void MatchesFilter_MustBeScript_ExcludesModProjects()
    {
        var mod = CreateProject(ProjectType.Mod);

        var matches = mod.MatchesFilter(string.Empty, mustBeMod: false, mustBeScript: true);

        Assert.That(matches, Is.False);
    }

    [Test]
    public void MatchesFilter_MustBeScript_IncludesProgrammableBlockProjects()
    {
        var script = CreateProject(ProjectType.ProgrammableBlock);

        var matches = script.MatchesFilter(string.Empty, mustBeMod: false, mustBeScript: true);

        Assert.That(matches, Is.True);
    }

    [Test]
    public void MatchesFilter_NoTypeFilters_MatchesAnyProjectType()
    {
        var script = CreateProject(ProjectType.ProgrammableBlock);
        var mod = CreateProject(ProjectType.Mod);

        Assert.Multiple(() =>
        {
            Assert.That(script.MatchesFilter(string.Empty, false, false), Is.True);
            Assert.That(mod.MatchesFilter(string.Empty, false, false), Is.True);
        });
    }

    [Test]
    public void MatchesFilter_SearchText_FiltersByNameSubstring()
    {
        var project = CreateProject(ProjectType.ProgrammableBlock, "MyAutopilot");

        Assert.Multiple(() =>
        {
            Assert.That(project.MatchesFilter("auto", false, false), Is.True);
            Assert.That(project.MatchesFilter("AUTO", false, false), Is.True, "search should be case-insensitive");
            Assert.That(project.MatchesFilter("Nope", false, false), Is.False);
        });
    }
}
