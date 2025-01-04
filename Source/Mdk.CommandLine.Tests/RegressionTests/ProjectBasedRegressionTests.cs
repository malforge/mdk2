using FakeItEasy;
using FluentAssertions;
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
        result.HasValue.Should().BeTrue();
        result!.Value.Length.Should().Be(1);
        var project = result.Value[0];
        project.Name.Should().NotBeNullOrEmpty();
        project.ProducedFiles.Should().NotBeEmpty();
        project.ProducedFiles.Should().ContainSingle(f => f.Id == "script.cs");
    }
}