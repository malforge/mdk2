using FakeItEasy;
using FluentAssertions;
using Mdk.CommandLine;
using Mdk.CommandLine.SharedApi;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.MdkProjects;

[TestFixture]
public class MdkProjectTests
{
    [Test]
    public async Task Test1()
    {
        var console = A.Fake<IConsole>();

        var n = 0;
        await foreach (var project in MdkProject.LoadAsync("TestData/LegacyScriptProject/LegacyScriptProject.csproj", console))
        {
            project.Type.Should().Be(MdkProjectType.LegacyProgrammableBlock);
            n++;
        }
        
        n.Should().Be(1);
    }
}