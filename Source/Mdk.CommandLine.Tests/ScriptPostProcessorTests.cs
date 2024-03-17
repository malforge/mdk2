using System.Collections.Immutable;
using FluentAssertions;
using Mdk.CommandLine.Commands.Pack;
using Mdk.CommandLine.IngameScript;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace MDK.CommandLine.Tests;

public abstract class ScriptPostProcessorTests<T> where T : class, IScriptPostprocessor, new()
{
    [Test]
    public async Task ProcessAsync_WhenRootIsNull_ReturnsDocument()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", "");
        var annotator = new T();
        var metadata = ScriptProjectMetadata.ForOptions(
            new PackParameters
            {
                MinifierLevel = MinifierLevel.None,
                TrimUnusedTypes = false,
                ProjectFile = @"A:\Fake\Path\Project.csproj",
                Output = @"A:\Fake\Path\Output"
            },
            new Version(2, 0, 0)
        ).Close();

        // Act
        var result = await annotator.ProcessAsync(document, metadata);

        // Assert
        result.Should().BeSameAs(document);
    }
}