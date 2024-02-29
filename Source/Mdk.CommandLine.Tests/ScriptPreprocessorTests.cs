using System.Collections.Immutable;
using FluentAssertions;
using Mdk.CommandLine.IngameScript;
using Mdk.CommandLine.IngameScript.Api;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace MDK.CommandLine.Tests;

public abstract class ScriptPreprocessorTests<T> where T : class, IScriptPreprocessor, new()
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
            new PackOptions
            {
                MinifierLevel = MinifierLevel.None,
                TrimUnusedTypes = false,
                ProjectFile = @"A:\Fake\Path\Project.csproj",
                Output = @"A:\Fake\Path\Output",
                ToClipboard = false,
                ListProcessors = false
            },
            new Version(2, 0, 0)
        ).Close();

        // Act
        var result = await annotator.ProcessAsync(document, metadata);

        // Assert
        result.Should().BeSameAs(document);
    }
}