using System.Linq;
using FakeItEasy;
using Mdk.CommandLine;
using Mdk.CommandLine.Shared.Api;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.RegressionTests;

/// <summary>
///     Packs a mod project and checks what actually lands in the deployed folder.
/// </summary>
/// <remarks>
///     A mod project declares <c>&lt;AdditionalFiles Include="**\*.ini"/&gt;</c> so the analyzers can read the
///     configuration, which once put <c>mdk.ini</c> and <c>mdk.local.ini</c> into every deployed mod, publishing
///     the author's local paths to the Workshop. The fixture carries both of those plus an ini that is genuine mod
///     content, so a regression in either direction fails here.
/// </remarks>
[TestFixture]
[RequiresSpaceEngineers]
public class ModContentDeploymentTests
{
    const string RelativeProjectPath = "TestData/ModContentTest/ModContentTest.csproj";

    [Test]
    public async Task Pack_ForMod_DeploysContentButNotMdkConfiguration()
    {
        var projectPath = Path.Combine(TestContext.CurrentContext.TestDirectory, RelativeProjectPath);
        Assert.That(File.Exists(projectPath), Is.True, $"Test project not found at {projectPath}");
        await BuildAsync(projectPath);

        var outputDirectory = Path.Combine(Path.GetTempPath(), $"mdk-modcontent-{Guid.NewGuid():N}");
        try
        {
            var peripherals = Program.Peripherals.Create()
                .WithInteraction(A.Fake<IInteraction>())
                .WithHttpClient(A.Fake<IHttpClient>(o => o.Strict()))
                .FromArguments(["pack", projectPath, "-output", outputDirectory])
                .Build();

            var packed = await Program.RunAsync(peripherals);
            Assert.That(packed, Is.Not.Null, "The project was not packed.");

            var deployed = Path.Combine(outputDirectory, "ModContentTest");
            Assert.That(Directory.Exists(deployed), Is.True, $"Nothing was deployed to {deployed}");
            var files = Directory.EnumerateFiles(deployed, "*", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(deployed, file).Replace('\\', '/'))
                .OrderBy(file => file)
                .ToArray();
            TestContext.Out.WriteLine("Deployed:" + Environment.NewLine + "  " + string.Join(Environment.NewLine + "  ", files));

            Assert.Multiple(() =>
            {
                Assert.That(files, Does.Contain("Data/Blocks.sbc"), "Mod content must be deployed.");
                Assert.That(files, Does.Contain("Data/settings.ini"), "An ini that is mod content must be deployed.");
                Assert.That(files, Does.Contain("thumb.png"), "The thumbnail must be deployed.");
                Assert.That(files, Does.Contain("Data/Scripts/ModContentTest/ModSession.cs"), "Mod scripts must be deployed.");
                Assert.That(files, Has.None.Contains("mdk.ini"), "MDK's own configuration must not be deployed.");
                Assert.That(files, Has.None.Contains("mdk.local.ini"), "The local configuration holds the author's paths and must not be deployed.");
                Assert.That(files, Has.None.StartsWith("bin/").And.None.StartsWith("obj/"), "Build output must not be deployed.");
            });
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, true);
        }
    }

    static async Task BuildAsync(string projectPath)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{projectPath}\" -c Release --verbosity quiet --nologo",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using var process = System.Diagnostics.Process.Start(startInfo);
        Assert.That(process, Is.Not.Null);
        var stdout = process!.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        await Task.WhenAll(stdout, stderr);
        Assert.That(process.ExitCode, Is.EqualTo(0), $"Build of {projectPath} failed:{Environment.NewLine}{stderr.Result}{stdout.Result}");
    }
}
