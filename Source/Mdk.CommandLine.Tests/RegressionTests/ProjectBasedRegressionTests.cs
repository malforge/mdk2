using FakeItEasy;
using Mdk.CommandLine;
using Mdk.CommandLine.Shared.Api;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.RegressionTests;

[TestFixture]
public class ProjectBasedRegressionTests
{
    [TestCase("TestData/Issue44/Issue44.csproj")]
    [TestCase("TestData/Issue50/Issue50.csproj")]
    public async Task Pack_ForProject_ShouldNotThrow(string path)
    {
        var peripherals = Program.Peripherals.Create()
            .WithInteraction(A.Fake<IInteraction>())
            .WithHttpClient(A.Fake<IHttpClient>(o => o.Strict()))
            .FromArguments([
                "pack",
                path,
                "-trace",
                "-dryrun"
            ])
            .Build();

        var result = await Program.RunAsync(peripherals);
        Assert.That(result.HasValue, Is.True);
        Assert.That(result!.Value.Length, Is.EqualTo(1));
        var project = result.Value[0];
        Assert.That(project.Name, Is.Not.Null.Or.Empty);
        Assert.That(project.ProducedFiles, Is.Not.Empty);
        Assert.That(project.ProducedFiles.Count(f => f.Id == "script.cs"), Is.EqualTo(1));
    }
}