using FakeItEasy;
using Mdk.CommandLine;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.SharedApi;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.RegressionTests;

[TestFixture]
public class RegressionIssue44Tests
{
    [Test]
    public async Task Issue44_ShouldNotThrow()
    {
        var peripherals = Program.Peripherals.Create()
            .WithInteraction(A.Fake<IInteraction>())
            .WithHttpClient(A.Fake<IHttpClient>(o => o.Strict()))
            /*.WithParameters(
                new Parameters
                {
                    Verb = Verb.Pack,
                    Log = null,
                    Trace = true,
                    Interactive = false,
                    PackVerb =
                    {
                        ProjectFile = "TestData/Issue44/Issue44.csproj",
                        GameBin = null,
                        Output = null,
                        MinifierLevel = MinifierLevel.Full,
                        Configuration = null
                    }
                }
            )*/
            .FromArguments([
                "pack",
                "TestData/Issue44/Issue44.csproj"
            ])
            .Build();

        await Program.RunAsync(peripherals);
    }
}