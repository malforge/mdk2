using System.Collections.Immutable;
using FluentAssertions;
using Mdk.CommandLine.Commands.PackScript;
using Mdk.CommandLine.IngameScript;
using Mdk.CommandLine.IngameScript.DefaultProcessors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptPostProcessors;

[TestFixture]
public class RegionAnnotatorTests : ScriptPostProcessorTests<RegionAnnotator>
{
    [Test]
    public void ProcessAsync_WithoutAnyAnnotations_ReturnsDocument()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", "class Program {}");
        var processor = new RegionAnnotator();
        var metadata = ScriptProjectMetadata.ForOptions(
            new PackScriptParameters
            {
                MinifierLevel = MinifierLevel.None,
                TrimUnusedTypes = false,
                ProjectFile = @"A:\Fake\Path\Project.csproj",
                Output = @"A:\Fake\Path\Output",
                Interactive = false
            },
            new Version(2, 0, 0)
        ).Close();

        // Act
        var result = processor.ProcessAsync(document, metadata).Result;

        // Assert
        result.Should().BeSameAs(document);
    }

    [Test]
    public async Task ProcessAsync_WithMDKRegions_ReturnsAnnotatedDocument()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            class UnAnnotatedClass
            {
            }

            #region mdk preserve
            class AnnotatedClass
            {
            }
            #endregion

            class ClassWithInternalRegion
            {
            #region mdk preserve
            public string AnnotatedProperty { get; set; }
            #endregion
            }
            """);
        var processor = new RegionAnnotator();
        var metadata = ScriptProjectMetadata.ForOptions(
            new PackScriptParameters
            {
                MinifierLevel = MinifierLevel.None,
                TrimUnusedTypes = false,
                ProjectFile = @"A:\Fake\Path\Project.csproj",
                Output = @"A:\Fake\Path\Output",
                Interactive = false
            },
            new Version(2, 0, 0)
        ).Close();

        // Act
        var result = processor.ProcessAsync(document, metadata).Result;

        // Assert
        result.Should().NotBeSameAs(document);
        var root = await result.GetSyntaxRootAsync();
        root.Should().NotBeNull();

        var unAnnotatedClass = root!.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "UnAnnotatedClass");
        var annotations = unAnnotatedClass.GetAnnotations("MDK");
        annotations.Should().BeEmpty();

        var annotatedClass = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "AnnotatedClass");
        annotations = annotatedClass.GetAnnotations("MDK");
        annotations.Should().NotBeEmpty();

        var classWithInternalRegion = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "ClassWithInternalRegion");
        annotations = classWithInternalRegion.GetAnnotations("MDK");
        annotations.Should().BeEmpty();
        var annotatedProperty = classWithInternalRegion.DescendantNodes().OfType<PropertyDeclarationSyntax>().First(p => p.Identifier.Text == "AnnotatedProperty");
        annotations = annotatedProperty.GetAnnotations("MDK");
        annotations.Should().NotBeEmpty();
    }
}