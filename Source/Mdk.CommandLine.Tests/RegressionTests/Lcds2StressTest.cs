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
        const string projectPath = "TestData/AutomaticLCDs2MDK2/AutomaticLCDs2MDK2.csproj";

        var restoreInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"restore \"{Path.Combine(TestContext.CurrentContext.TestDirectory, projectPath)}\" --verbosity quiet",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var restoreProc = System.Diagnostics.Process.Start(restoreInfo)!;
        var stdoutTask = restoreProc.StandardOutput.ReadToEndAsync();
        var stderrTask = restoreProc.StandardError.ReadToEndAsync();
        await restoreProc.WaitForExitAsync();
        await Task.WhenAll(stdoutTask, stderrTask);
        Assert.That(restoreProc.ExitCode, Is.EqualTo(0), $"Restore failed:\n{stderrTask.Result}{stdoutTask.Result}");

        var peripherals = Program.Peripherals.Create()
            .WithInteraction(A.Fake<IInteraction>())
            .WithHttpClient(A.Fake<IHttpClient>(o => o.Strict()))
            .FromArguments([
                "pack",
                projectPath
            ])
            .Build();

        await Program.RunAsync(peripherals);
    }
}