using System.Linq;
using FakeItEasy;
using Mdk.CommandLine;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.RegressionTests;

[TestFixture]
public class ProjectBasedRegressionTests
{
    [TestCase("TestData/Issue44/Issue44.csproj")]
    [TestCase("TestData/Issue50/Issue50.csproj")]
    [TestCase("TestData/Issue98/Issue98.csproj")]
    public async Task Pack_ForProject_ShouldNotThrow(string path)
    {
        var project = await PackProjectAsync(path);
        Assert.That(project.Name, Is.Not.Null.Or.Empty);
        Assert.That(project.ProducedFiles, Is.Not.Empty);
        Assert.That(project.ProducedFiles.Count(f => f.Id == "script.cs"), Is.EqualTo(1));
    }

    [Test]
    public async Task Pack_ForIssue90_KeepsTrailingPreservedCommentWithField()
    {
        var project = await PackProjectAsync("TestData/Issue90/Issue90.csproj");
        var script = project.ProducedFiles.Single(f => f.Id == "script.cs").Content;

        Assert.That(script, Is.Not.Null);

        var fieldCommentIndex = script!.IndexOf("// This is comment after test var", StringComparison.Ordinal);
        var mainIndex = script.IndexOf("Main(", StringComparison.Ordinal);
        var fieldIndex = script.IndexOf("test = true", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(fieldCommentIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(fieldIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(mainIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(fieldCommentIndex, Is.LessThan(mainIndex));
            Assert.That(fieldCommentIndex, Is.GreaterThan(fieldIndex));
        });
    }

    [Test]
    public async Task Pack_ForIssue129_ShouldNotThrow()
    {
        var project = await PackProjectAsync("TestData/Issue129/Issue129.csproj");
        Assert.That(project.Name, Is.Not.Null.Or.Empty);
        Assert.That(project.ProducedFiles.Count(f => f.Id == "script.cs"), Is.EqualTo(1));
    }

    [Test]
    public async Task Pack_ForIssue130_PreservesEnumNamesInsidePreserveRegion()
    {
        var project = await PackProjectAsync("TestData/Issue130/Issue130.csproj");
        var script = project.ProducedFiles.Single(f => f.Id == "script.cs").Content;

        Assert.That(script, Is.Not.Null);

        var syntaxRoot = CSharpSyntaxTree.ParseText(script!).GetRoot();
        var enumDeclaration = syntaxRoot.DescendantNodes().OfType<EnumDeclarationSyntax>().Single();
        var enumMembers = enumDeclaration.Members.Select(m => m.Identifier.ValueText).ToArray();
        var enumReferences = syntaxRoot.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Where(m => m.Expression is IdentifierNameSyntax { Identifier.ValueText: "Beans" })
            .Select(m => m.Name.Identifier.ValueText)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(enumDeclaration.Identifier.ValueText, Is.EqualTo("Beans"));
            Assert.That(enumMembers, Is.EqualTo(new[] { "Pinto", "Kidney", "Red" }));
            Assert.That(enumReferences, Is.EqualTo(new[] { "Pinto", "Kidney" }));
        });
    }

    [Test]
    public async Task Pack_ForIssue143_DefaultConfiguration_OmitsUserConstantBlocks()
    {
        var project = await PackProjectAsync("TestData/Issue143/Issue143.csproj");
        var script = project.ProducedFiles.Single(f => f.Id == "script.cs").Content;

        Assert.That(script, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Not.Contain("IncludedAsUserConstant"),
                "THIS_SHOULD_BE_INCLUDED is only defined under MyRandomConfiguration; should be absent in the default Release pack.");
            Assert.That(script, Does.Not.Contain("NotIncludedAsBuildName"),
                "The build configuration name must not be treated as a #if symbol.");
        });
    }

    [Test]
    public async Task Pack_ForIssue143_CustomConfiguration_IncludesUserConstantButNotConfigurationName()
    {
        var project = await PackProjectAsync("TestData/Issue143/Issue143.csproj", "MyRandomConfiguration");
        var script = project.ProducedFiles.Single(f => f.Id == "script.cs").Content;

        Assert.That(script, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("IncludedAsUserConstant"),
                "THIS_SHOULD_BE_INCLUDED is defined via DefineConstants for MyRandomConfiguration and must drive #if.");
            Assert.That(script, Does.Not.Contain("NotIncludedAsBuildName"),
                "Build configuration name must never be treated as a #if symbol (regression for #143).");
        });
    }

    static async Task<PackedProject> PackProjectAsync(string path, string? configuration = null)
    {
        var fullPath = Path.Combine(TestContext.CurrentContext.TestDirectory, path);
        if (File.Exists(fullPath))
        {
            var restoreArgs = $"restore \"{fullPath}\" --verbosity quiet";
            if (configuration is not null)
                restoreArgs += $" -p:Configuration={configuration}";
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = restoreArgs,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = System.Diagnostics.Process.Start(startInfo);
            Assert.That(process, Is.Not.Null);
            var standardOutputTask = process!.StandardOutput.ReadToEndAsync();
            var standardErrorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            await Task.WhenAll(standardOutputTask, standardErrorTask);
            Assert.That(process.ExitCode, Is.EqualTo(0), $"{standardErrorTask.Result}{standardOutputTask.Result}");
        }

        var args = new List<string> { "pack", path, "-trace", "-dryrun" };
        if (configuration is not null)
        {
            args.Add("-configuration");
            args.Add(configuration);
        }

        var peripherals = Program.Peripherals.Create()
            .WithInteraction(A.Fake<IInteraction>())
            .WithHttpClient(A.Fake<IHttpClient>(o => o.Strict()))
            .FromArguments(args.ToArray())
            .Build();

        var result = await Program.RunAsync(peripherals);
        Assert.That(result.HasValue, Is.True);
        Assert.That(result!.Value.Length, Is.EqualTo(1));
        return result.Value[0];
    }
}
