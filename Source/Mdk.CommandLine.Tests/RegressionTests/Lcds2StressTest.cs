using FakeItEasy;
using Mdk.CommandLine;
using Mdk.CommandLine.Shared.Api;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.RegressionTests;

[TestFixture]
[RequiresFile("TestData/AutomaticLCDs2MDK2/AutomaticLCDs2MDK2.csproj")]
public class Lcds2StressTest
{
    [Test]
    public async Task Lcds2StressTest_ShouldNotThrow()
    {
        var peripherals = Program.Peripherals.Create()
            .WithInteraction(A.Fake<IInteraction>())
            .WithHttpClient(A.Fake<IHttpClient>(o => o.Strict()))
            .FromArguments([
                "pack",
                "TestData/AutomaticLCDs2MDK2/AutomaticLCDs2MDK2.csproj"
            ])
            .Build();

        await Program.RunAsync(peripherals);
    }
}