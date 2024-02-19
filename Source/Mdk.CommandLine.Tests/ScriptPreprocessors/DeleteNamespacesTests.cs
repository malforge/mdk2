using FluentAssertions;
using Mdk.CommandLine.IngameScript;
using Mdk.CommandLine.IngameScript.DefaultProcessors;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptPreprocessors;

[TestFixture]
public class DeleteNamespacesTests: ScriptPreprocessorTests<DeleteNamespaces>
{
    [Test]
    public async Task ProcessAsync_WithNoNamespace_ReturnsDocument()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", "class Program {}");
        var processor = new DeleteNamespaces();
        var metadata = new ScriptProjectMetadata
        {
            MdkProjectVersion = new Version(2, 0, 0),
            ProjectDirectory = @"A:\Fake\Path",
            OutputDirectory = @"A:\Fake\Path\Output",
            Macros = new Dictionary<string, string>()
        };
        
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
        var metadata = new ScriptProjectMetadata
        {
            MdkProjectVersion = new Version(2, 0, 0),
            ProjectDirectory = @"A:\Fake\Path",
            OutputDirectory = @"A:\Fake\Path\Output",
            Macros = new Dictionary<string, string>()
        };
        
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
        var metadata = new ScriptProjectMetadata
        {
            MdkProjectVersion = new Version(2, 0, 0),
            ProjectDirectory = @"A:\Fake\Path",
            OutputDirectory = @"A:\Fake\Path\Output",
            Macros = new Dictionary<string, string>()
        };
        
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