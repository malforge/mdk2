using FakeItEasy;
using Mdk.CommandLine;
using Mdk.CommandLine.Shared.Api;
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
            Assert.That(project.Type, Is.EqualTo(MdkProjectType.LegacyProgrammableBlock));
            n++;
        }
        
        Assert.That(n, Is.EqualTo(1));
    }
}