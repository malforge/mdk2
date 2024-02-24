using System.Collections.Immutable;
using FluentAssertions;
using Mdk.CommandLine.IngameScript;
using Mdk.CommandLine.IngameScript.DefaultProcessors;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptPostProcessors;

public class PartialMergerTests : ScriptPostProcessorTests<PartialMerger>
{
    [Test]
    public async Task ProcessAsync_WithPartialClasses_ReturnsDocumentWithMergedPartialClasses()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            partial class Program
            {
                void Method1() {}
            }
            partial class Program
            {
                void Method2() {}
            }
            """);
        var processor = new PartialMerger();
        var metadata = new ScriptProjectMetadata
        {
            MdkProjectVersion = new Version(2, 0, 0),
            ProjectDirectory = @"A:\Fake\Path",
            OutputDirectory = @"A:\Fake\Path\Output",
            Macros = ImmutableDictionary<string, string>.Empty,
            PreprocessorMacros = ImmutableHashSet.Create<string>()
        };

        // Act
        var result = await processor.ProcessAsync(document, metadata);

        // Assert
        var text = await result.GetTextAsync();
        text.ToString().Replace("\r\n", "\n").Should().Be(
            """
            class Program
            {
                void Method1() {}
                void Method2() {}
            }
            
            """.Replace("\r\n", "\n"));
    }
    
    [Test]
    public async Task ProcessAsync_WithPartialStructs_ReturnsDocumentWithMergedPartialStructs()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            partial struct Program
            {
                void Method1() {}
            }
            partial struct Program
            {
                void Method2() {}
            }
            """);
        var processor = new PartialMerger();
        var metadata = new ScriptProjectMetadata
        {
            MdkProjectVersion = new Version(2, 0, 0),
            ProjectDirectory = @"A:\Fake\Path",
            OutputDirectory = @"A:\Fake\Path\Output",
            Macros = ImmutableDictionary<string, string>.Empty,
            PreprocessorMacros = ImmutableHashSet.Create<string>()
        };

        // Act
        var result = await processor.ProcessAsync(document, metadata);

        // Assert
        var text = await result.GetTextAsync();
        text.ToString().Replace("\r\n", "\n").Should().Be(
            """
            struct Program
            {
                void Method1() {}
                void Method2() {}
            }
            """.Replace("\r\n", "\n"));
    }
    
    [Test]
    public async Task ProcessAsync_WithPartialInterfaces_ReturnsDocumentWithMergedPartialInterfaces()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            partial interface IProgram
            {
                void Method1();
            }
            partial interface IProgram
            {
                void Method2();
            }
            """);
        var processor = new PartialMerger();
        var metadata = new ScriptProjectMetadata
        {
            MdkProjectVersion = new Version(2, 0, 0),
            ProjectDirectory = @"A:\Fake\Path",
            OutputDirectory = @"A:\Fake\Path\Output",
            Macros = ImmutableDictionary<string, string>.Empty,
            PreprocessorMacros = ImmutableHashSet.Create<string>()
        };

        // Act
        var result = await processor.ProcessAsync(document, metadata);

        // Assert
        var text = await result.GetTextAsync();
        text.ToString().Replace("\r\n", "\n").Should().Be(
            """
            interface IProgram
            {
                void Method1();
                void Method2();
            }
            """.Replace("\r\n", "\n"));
    }
    
    [Test]
    public async Task ProcessAsync_WithMixedPartialTypesAndNonPartialTypes_ReturnsDocumentWithMergedPartialTypesAndNonPartialTypes()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            partial class PartialClassOne
            {
                void Method1() {}
            }
            class NonPartialClass
            {
                void Method2() {}
            }
            partial class PartialClassOne
            {
                void Method3() {}
            }
            """);
        var processor = new PartialMerger();
        var metadata = new ScriptProjectMetadata
        {
            MdkProjectVersion = new Version(2, 0, 0),
            ProjectDirectory = @"A:\Fake\Path",
            OutputDirectory = @"A:\Fake\Path\Output",
            Macros = ImmutableDictionary<string, string>.Empty,
            PreprocessorMacros = ImmutableHashSet.Create<string>()
        };

        // Act
        var result = await processor.ProcessAsync(document, metadata);

        // Assert
        var text = await result.GetTextAsync();
        text.ToString().Replace("\r\n", "\n").Should().Be(
            """
            class PartialClassOne
            {
                void Method1() {}
                void Method3() {}
            }
            class NonPartialClass
            {
                void Method2() {}
            }
            
            """.Replace("\r\n", "\n"));
    }
}