using FluentAssertions;
using Mdk.CommandLine.Commands.Pack;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptPreprocessors;

[TestFixture]
public class DeleteNamespacesTests : ScriptPreprocessorTests<DeleteNamespaces>
{
    [Test]
    public async Task ProcessAsync_WithNoNamespace_ReturnsDocument()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", "class Program {}");
        var processor = new DeleteNamespaces();
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
        var result = await processor.ProcessAsync(document, metadata);

        // Assert
        result.Should().BeSameAs(document);
    }

    [Test]
    public async Task ProcessAsync_WithNamespace_ReturnsDocumentWithoutNamespace()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", "namespace TestNamespace { class Program {} }");
        var processor = new DeleteNamespaces();
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
        var result = await processor.ProcessAsync(document, metadata);

        // Assert
        var text = await result.GetTextAsync();
        text.ToString().Replace("\r\n", "\n").Should().Be(" class Program {}".Replace("\r\n", "\n"));
    }

    [Test]
    public async Task ProcessAsync_WithNamespace_WillUnindent()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            namespace TestNamespace
            {
                class Program
                {
                }
            }
            """);
        var processor = new DeleteNamespaces();
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
        var result = await processor.ProcessAsync(document, metadata);

        // Assert
        var text = await result.GetTextAsync();
        text.ToString().Replace("\r\n", "\n").Should().Be("""
                                                          class Program
                                                          {
                                                          }

                                                          """.Replace("\r\n", "\n"));
    }
}