using System.Collections.Immutable;
using FluentAssertions;
using Mdk.CommandLine.IngameScript;
using Mdk.CommandLine.IngameScript.DefaultProcessors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptPostProcessors;

[TestFixture]
public class TypeSorterTests : ScriptPostProcessorTests<TypeSorter>
{
    [Test]
    public async Task ProcessAsync_WhenProgramTypeIsFirst_ReturnsDocument()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", "class Program {}");
        var processor = new TypeSorter();
        var metadata = ScriptProjectMetadata.ForOptions(
            new PackOptions
            {
                MinifierLevel = MinifierLevel.None,
                TrimUnusedTypes = false,
                ProjectFile = @"A:\Fake\Path\Project.csproj",
                Output = @"A:\Fake\Path\Output",
                Interactive = false,
                ListProcessors = false
            },
            new Version(2, 0, 0)
        ).Close();

        // Act
        var result = await processor.ProcessAsync(document, metadata);

        // Assert
        result.Should().BeSameAs(document);
    }

    [Test]
    public async Task ProcessAsync_WhenProgramTypeIsNotFirst_ReturnsDocumentWithProgramTypeFirst()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", "class A {} class Program {} class B {}");
        var processor = new TypeSorter();
        var metadata = ScriptProjectMetadata.ForOptions(
            new PackOptions
            {
                MinifierLevel = MinifierLevel.None,
                TrimUnusedTypes = false,
                ProjectFile = @"A:\Fake\Path\Project.csproj",
                Output = @"A:\Fake\Path\Output",
                Interactive = false,
                ListProcessors = false
            },
            new Version(2, 0, 0)
        ).Close();

        // Act
        var result = await processor.ProcessAsync(document, metadata);

        // Assert
        var syntaxRoot = await result.GetSyntaxRootAsync();
        syntaxRoot.Should().NotBeNull();
        syntaxRoot!.ChildNodes().OfType<TypeDeclarationSyntax>().Select(t => t.Identifier.Text).Should().Equal("Program", "A", "B");
    }
}