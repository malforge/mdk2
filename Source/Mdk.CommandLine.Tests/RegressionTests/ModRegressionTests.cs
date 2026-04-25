using FakeItEasy;
using Mdk.CommandLine;
using Mdk.CommandLine.Shared.Api;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.RegressionTests;

[TestFixture]
public class ModRegressionTests
{
    [Test]
    public async Task Pack_ForIssue139_InlinesSourceGeneratedDocuments()
    {
        const string relativeProjectPath = "TestData/Issue139/Issue139.csproj";
        var fullProjectPath = Path.Combine(TestContext.CurrentContext.TestDirectory, relativeProjectPath);
        Assert.That(File.Exists(fullProjectPath), Is.True, $"Test project not found at {fullProjectPath}");

        await BuildAsync(fullProjectPath);

        var outputDir = Path.Combine(Path.GetTempPath(), $"mdk-issue139-{Guid.NewGuid():N}");
        try
        {
            var peripherals = Program.Peripherals.Create()
                .WithInteraction(A.Fake<IInteraction>())
                .WithHttpClient(A.Fake<IHttpClient>(o => o.Strict()))
                .FromArguments([
                    "pack",
                    relativeProjectPath,
                    "-output", outputDir,
                    "-trace"
                ])
                .Build();

            await Program.RunAsync(peripherals);

            var expected = Path.Combine(outputDir, "Issue139", "Data", "Scripts", "Issue139", "MdkGenerated", "HelloFromGenerator.g.cs");
            Assert.That(File.Exists(expected), Is.True, $"Generated file missing at {expected}");

            var content = await File.ReadAllTextAsync(expected);
            Assert.That(content, Does.Contain("Hello from source generator"));
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }

    static async Task BuildAsync(string projectPath)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{projectPath}\" --verbosity quiet --nologo",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var process = System.Diagnostics.Process.Start(startInfo);
        Assert.That(process, Is.Not.Null);
        var stdoutTask = process!.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        await Task.WhenAll(stdoutTask, stderrTask);
        Assert.That(process.ExitCode, Is.EqualTo(0), $"Build of {projectPath} failed:\n{stderrTask.Result}{stdoutTask.Result}");
    }
}
