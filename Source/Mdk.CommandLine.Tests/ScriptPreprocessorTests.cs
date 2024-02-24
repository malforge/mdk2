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
        var metadata = new ScriptProjectMetadata
        {
            MdkProjectVersion = new Version(2, 0, 0),
            ProjectDirectory = @"A:\Fake\Path",
            OutputDirectory = @"A:\Fake\Path\Output",
            Macros = ImmutableDictionary<string, string>.Empty,
            PreprocessorMacros = ImmutableHashSet.Create<string>()
        };

        // Act
        var result = await annotator.ProcessAsync(document, metadata);

        // Assert
        result.Should().BeSameAs(document);
    }
}