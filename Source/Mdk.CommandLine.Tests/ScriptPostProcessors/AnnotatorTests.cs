using FluentAssertions;
using Mdk.CommandLine.IngameScript;
using Mdk.CommandLine.IngameScript.DefaultProcessors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptPostProcessors;

[TestFixture]
public class AnnotatorTests : ScriptPostProcessorTests<Annotator>
{
    [Test]
    public void ProcessAsync_WithoutAnyAnnotations_ReturnsDocument()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", "class Program {}");
        var processor = new Annotator();
        var metadata = new ScriptProjectMetadata
        {
            MdkProjectVersion = new Version(2, 0, 0),
            ProjectDirectory = @"A:\Fake\Path",
            OutputDirectory = @"A:\Fake\Path\Output",
            Macros = new Dictionary<string, string>()
        };

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
        var processor = new Annotator();
        var metadata = new ScriptProjectMetadata
        {
            MdkProjectVersion = new Version(2, 0, 0),
            ProjectDirectory = @"A:\Fake\Path",
            OutputDirectory = @"A:\Fake\Path\Output",
            Macros = new Dictionary<string, string>()
        };

        // Act
        var result = processor.ProcessAsync(document, metadata).Result;

        // Assert
        result.Should().NotBeSameAs(document);
        var root = await result.GetSyntaxRootAsync();
        root.Should().NotBeNull();

        var unAnnotatedClass = root!.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "UnAnnotatedClass");
        var annotations = unAnnotatedClass.GetAnnotations("mdk");
        annotations.Should().BeEmpty();

        var annotatedClass = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "AnnotatedClass");
        annotations = annotatedClass.GetAnnotations("mdk");
        annotations.Should().NotBeEmpty();

        var classWithInternalRegion = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "ClassWithInternalRegion");
        annotations = classWithInternalRegion.GetAnnotations("mdk");
        annotations.Should().BeEmpty();
        var annotatedProperty = classWithInternalRegion.DescendantNodes().OfType<PropertyDeclarationSyntax>().First(p => p.Identifier.Text == "AnnotatedProperty");
        annotations = annotatedProperty.GetAnnotations("mdk");
        annotations.Should().NotBeEmpty();
    }
}