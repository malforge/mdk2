using FakeItEasy;
using FluentAssertions;
using Mdk.CommandLine.IngameScript;
using Mdk.CommandLine.IngameScript.DefaultProcessors;
using Mdk.CommandLine.SharedApi;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptComposers;

[TestFixture]
public class ComposerTests
{
    [Test]
    public async Task ComposeAsync_WithContent_ReturnsDocumentAsString()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            class Program1
            {}

            class Program2
            {}

            """);

        var composer = new Composer();
        var metadata = new ScriptProjectMetadata
        {
            MdkProjectVersion = new Version(2, 0, 0),
            ProjectDirectory = @"A:\Fake\Path",
            OutputDirectory = @"A:\Fake\Path\Output",
            Macros = new Dictionary<string, string>()
        };
        var expected = "class Program1 {}\n\nclass Program2 {}\n\n";
        var console = A.Fake<IConsole>();

        // Act
        var result = await composer.ComposeAsync(document, console, metadata);

        // Assert
        result.ToString().Replace("\r\n", "\n").Should().Be(expected.Replace("\r\n", "\n"));
    }
}