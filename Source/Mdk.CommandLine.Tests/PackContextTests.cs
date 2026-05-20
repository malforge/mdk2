using Mdk.CommandLine.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace MDK.CommandLine.Tests;

[TestFixture]
public class PackContextTests
{
    [Test]
    public void ResolvePreprocessorSymbols_ReturnsParseOptionsSymbols()
    {
        using var workspace = new AdhocWorkspace();
        var projectInfo = ProjectInfo.Create(
                ProjectId.CreateNewId(),
                VersionStamp.Default,
                "TestProject",
                "TestProject",
                LanguageNames.CSharp,
                parseOptions: new CSharpParseOptions().WithPreprocessorSymbols("DEBUG", "THIS_SHOULD_BE_INCLUDED"));
        var project = workspace.AddProject(projectInfo);

        var symbols = PackContext.ResolvePreprocessorSymbols(project);

        Assert.That(symbols, Is.EquivalentTo(new[] { "DEBUG", "THIS_SHOULD_BE_INCLUDED" }));
    }

    [Test]
    public void ResolvePreprocessorSymbols_DoesNotIncludeConfigurationName()
    {
        using var workspace = new AdhocWorkspace();
        var projectInfo = ProjectInfo.Create(
                ProjectId.CreateNewId(),
                VersionStamp.Default,
                "TestProject",
                "TestProject",
                LanguageNames.CSharp,
                parseOptions: new CSharpParseOptions().WithPreprocessorSymbols("DEBUG"));
        var project = workspace.AddProject(projectInfo);

        var symbols = PackContext.ResolvePreprocessorSymbols(project);

        Assert.That(symbols, Does.Not.Contain("Release"));
        Assert.That(symbols, Does.Not.Contain("Debug"));
        Assert.That(symbols, Does.Not.Contain("MyRandomConfiguration"));
    }

    [Test]
    public void ResolvePreprocessorSymbols_WhenNoParseOptions_ReturnsEmpty()
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);

        var symbols = PackContext.ResolvePreprocessorSymbols(project);

        Assert.That(symbols, Is.Empty);
    }

    [Test]
    public void ResolvePreprocessorSymbols_IsCaseInsensitive()
    {
        using var workspace = new AdhocWorkspace();
        var projectInfo = ProjectInfo.Create(
                ProjectId.CreateNewId(),
                VersionStamp.Default,
                "TestProject",
                "TestProject",
                LanguageNames.CSharp,
                parseOptions: new CSharpParseOptions().WithPreprocessorSymbols("MY_SYMBOL"));
        var project = workspace.AddProject(projectInfo);

        var symbols = PackContext.ResolvePreprocessorSymbols(project);

        Assert.That(symbols.Contains("my_symbol"), Is.True);
        Assert.That(symbols.Contains("My_Symbol"), Is.True);
    }
}
