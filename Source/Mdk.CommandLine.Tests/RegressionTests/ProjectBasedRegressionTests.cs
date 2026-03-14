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

    static async Task<PackedProject> PackProjectAsync(string path)
    {
        var fullPath = Path.Combine(TestContext.CurrentContext.TestDirectory, path);
        if (File.Exists(fullPath))
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"restore \"{fullPath}\" --verbosity quiet",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var process = System.Diagnostics.Process.Start(startInfo);
            Assert.That(process, Is.Not.Null);
            process!.WaitForExit();
            Assert.That(process.ExitCode, Is.EqualTo(0), await process.StandardError.ReadToEndAsync());
        }

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
        return result.Value[0];
    }
}
