using System.Collections.Immutable;
using FakeItEasy;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;
using Mdk.CommandLine.Shared;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptPostProcessors;

[TestFixture]
public class RegionAnnotatorTests : DocumentProcessorTests<RegionAnnotator>
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        // Act
        var result = processor.ProcessAsync(document, context).Result;

        // Assert
        Assert.That(result, Is.EqualTo(document));
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        // Act
        var result = processor.ProcessAsync(document, context).Result;

        // Assert
        Assert.That(result, Is.Not.SameAs(document));
        var root = await result.GetSyntaxRootAsync();
        Assert.That(root, Is.Not.Null);
        
        var unAnnotatedClass = root!.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "UnAnnotatedClass");
        var annotations = unAnnotatedClass.GetAnnotations("MDK");
        Assert.That(annotations, Is.Empty);
        
        var annotatedClass = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "AnnotatedClass");
        annotations = annotatedClass.GetAnnotations("MDK");
        Assert.That(annotations, Is.Not.Empty);
        
        var classWithInternalRegion = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "ClassWithInternalRegion");
        annotations = classWithInternalRegion.GetAnnotations("MDK");
        Assert.That(annotations, Is.Empty);
        var annotatedProperty = classWithInternalRegion.DescendantNodes().OfType<PropertyDeclarationSyntax>().First(p => p.Identifier.Text == "AnnotatedProperty");
        annotations = annotatedProperty.GetAnnotations("MDK");
        Assert.That(annotations, Is.Not.Empty);
    }

    [Test]
    public async Task ProcessAsync_ReplacesMacrosInCommentsAndStrings()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            class MacroSandbox
            {
                // single $TEST$
                /* multi-line $TEST$ */
                /// <summary>xml $TEST$</summary>
                // should not replace $UNMATCHED$
                void Method()
                {
                    var plainString = "$TEST$";
                    var notMacroString = "$UNMATCHED$";
                }
                
                // Regions should also support replacements inside
                #region mdk preserve
                public string InsideRegion = "$TEST$";
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
                Output = @"A:\Fake\Path\Output",
                Macros =
                {
                    ["$TEST$"] = "REPLACED"
                }
            }
        };
        var context = new PackContext(
            parameters,
            A.Fake<IConsole>(o => o.Strict()),
            A.Fake<IInteraction>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        // Act
        var result = await processor.ProcessAsync(document, context);
        var text = (await result.GetTextAsync()).ToString();

        // Assert
        Assert.That(text, Does.Contain("// single REPLACED"));
        Assert.That(text, Does.Contain("/* multi-line REPLACED */"));
        Assert.That(text, Does.Contain("/// <summary>xml REPLACED</summary>"));
        Assert.That(text, Does.Contain("// should not replace $UNMATCHED$"));
        Assert.That(text, Does.Contain("var plainString = \"REPLACED\";"));
        Assert.That(text, Does.Contain("var notMacroString = \"$UNMATCHED$\";"));
        Assert.That(text, Does.Contain("public string InsideRegion = \"REPLACED\";"));
    }
}
