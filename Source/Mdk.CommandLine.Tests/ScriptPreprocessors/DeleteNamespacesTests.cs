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

namespace MDK.CommandLine.Tests.ScriptPreprocessors;

[TestFixture]
public class DeleteNamespacesTests : DocumentProcessorTests<DeleteNamespaces>
{
    [Test]
    public async Task ProcessAsync_WithNoNamespace_ReturnsDocument()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", "class Program {}");
        var processor = new DeleteNamespaces();
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
        var result = await processor.ProcessAsync(document, context);

        // Assert
        Assert.That(result, Is.SameAs(document));
    }

    [Test]
    public async Task ProcessAsync_WithNamespace_ReturnsDocumentWithoutNamespace()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", "namespace TestNamespace { class Program {} }");
        var processor = new DeleteNamespaces();
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
        var result = await processor.ProcessAsync(document, context);

        // Assert
        var text = await result.GetTextAsync();
        Assert.That(text.ToString().Replace("\r\n", "\n"), Is.EqualTo(" class Program {}".Replace("\r\n", "\n")));
    }

    [Test]
    public async Task ProcessAsync_WithNamespaceWithDefine_ReturnsDocumentWithoutNamespace()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        //var document = project.AddDocument("TestDocument", "namespace TestNamespace { class Program {} }");
        var document = project.AddDocument("TestDocument", "#define LABEL\r\nnamespace TestNamespace { class Program {} }");
        var processor = new DeleteNamespaces();
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
        var result = await processor.ProcessAsync(document, context);

        // Assert
        var text = await result.GetTextAsync();
        Assert.That(text.ToString().Replace("\r\n", "\n"), Is.EqualTo(" class Program {}".Replace("\r\n", "\n")));
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
        var result = await processor.ProcessAsync(document, context);

        // Assert
        var text = await result.GetTextAsync();
        Assert.That(text.ToString().Replace("\r\n", "\n"), Is.EqualTo("""
                                                          class Program
                                                          {
                                                          }
                                                          
                                                          """.Replace("\r\n", "\n")));
    }

    [Test]
    public async Task ProcessAsync_WithPreserveRegionAroundTopLevelMember_AnnotatesPreservedMember()
    {
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            namespace TestNamespace
            {
                #region mdk preserve
                enum PreservedEnum
                {
                    Alpha
                }
                #endregion
            }
            """);
        var processor = new DeleteNamespaces();
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

        var result = await processor.ProcessAsync(document, context);
        var root = await result.GetSyntaxRootAsync();
        var preservedEnum = root!.DescendantNodes().OfType<EnumDeclarationSyntax>().Single();

        Assert.That(preservedEnum.Identifier.Text, Is.EqualTo("PreservedEnum"));
        Assert.That(preservedEnum.ShouldBePreserved(), Is.True);
        Assert.That(preservedEnum.Members.Single().ShouldBePreserved(), Is.True);
    }
}
