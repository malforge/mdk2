using System.Collections.Immutable;
using FakeItEasy;
using FluentAssertions;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;
using Mdk.CommandLine.SharedApi;
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
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb = 
            {
                MinifierLevel = MinifierLevel.None,
                ProjectFile = @"A:\Fake\Path\Project.csproj",
                Output = @"A:\Fake\Path\Output"
            }
        };
        var context = new PackContext(
            parameters,
            A.Fake<IConsole>(o => o.Strict()),
            A.Fake<IInteraction>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        // Act
        var result = processor.ProcessAsync(document, context).Result;

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
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb = 
            {
                MinifierLevel = MinifierLevel.None,
                ProjectFile = @"A:\Fake\Path\Project.csproj",
                Output = @"A:\Fake\Path\Output"
            }
        };
        var context = new PackContext(
            parameters,
            A.Fake<IConsole>(o => o.Strict()),
            A.Fake<IInteraction>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        // Act
        var result = processor.ProcessAsync(document, context).Result;

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