using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Mdk.CommandLine.IngameScript;
using Mdk.CommandLine.IngameScript.DefaultProcessors;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptPreprocessors;

[TestFixture]
[SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
public class PreprocessorConditionalsTests : ScriptPreprocessorTests<PreprocessorConditionals>
{
    [Test]
    public async Task ProcessAsync_WithSimpleDebugBlock_RemovesDebugBlock()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            using System;

            public class Program
            {
                public void Main()
                {
                    #if DEBUG
                    var x = 1;
                    #endif
                    var y = 2;
                }
            }
            """);
        var processor = new PreprocessorConditionals();
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
        var text = await result.GetTextAsync();
        text.ToString().Replace("\r\n", "\n").Should().Be(
            """
            using System;

            public class Program
            {
                public void Main()
                {
                    var y = 2;
                }
            }
            """.Replace("\r\n", "\n"));
    }

    [Test]
    public async Task ProcessAsync_WithSimpleDebugBlockWithDebugSymbol_ShouldLeaveBlock()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            using System;

            public class Program
            {
                public void Main()
                {
                    #if DEBUG
                    var x = 1;
                    #endif
                    var y = 2;
                }
            }
            """);
        var processor = new PreprocessorConditionals();
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
            ).WithAdditionalPreprocessorMacros(new[] { "DEBUG" })
            .Close();

        // Act
        var result = await processor.ProcessAsync(document, metadata);

        // Assert
        var text = await result.GetTextAsync();
        text.ToString().Replace("\r\n", "\n").Should().Be(
            """
            using System;

            public class Program
            {
                public void Main()
                {
                    var x = 1;
                    var y = 2;
                }
            }
            """.Replace("\r\n", "\n"));
    }

    [Test]
    public async Task ProcessAsync_WithIfElse_ShouldLeaveElse()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            using System;

            public class Program
            {
                public void Main()
                {
                    #if DEBUG
                    var x = 1;
                    #else
                    var y = 2;
                    #endif
                }
            }
            """);
        var processor = new PreprocessorConditionals();
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
        var text = await result.GetTextAsync();
        text.ToString().Replace("\r\n", "\n").Should().Be(
            """
            using System;

            public class Program
            {
                public void Main()
                {
                    var y = 2;
                }
            }
            """.Replace("\r\n", "\n"));
    }

    [Test]
    public async Task ProcessAsync_WithIfNot_ShouldLeaveBlock()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            using System;

            public class Program
            {
                public void Main()
                {
                    #if !DEBUG
                    var x = 1;
                    #endif
                    var y = 2;
                }
            }
            """);
        var processor = new PreprocessorConditionals();
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
        var text = await result.GetTextAsync();
        text.ToString().Replace("\r\n", "\n").Should().Be(
            """
            using System;

            public class Program
            {
                public void Main()
                {
                    var x = 1;
                    var y = 2;
                }
            }
            """.Replace("\r\n", "\n"));
    }

    [Test]
    public async Task ProcessAsync_WithIfNotElse_ShouldLeaveBlock()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            using System;

            public class Program
            {
                public void Main()
                {
                    #if !DEBUG
                    var x = 1;
                    #else
                    var y = 2;
                    #endif
                    var z = 3;
                }
            }
            """);
        var processor = new PreprocessorConditionals();
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
        var text = await result.GetTextAsync();
        text.ToString().Replace("\r\n", "\n").Should().Be(
            """
            using System;

            public class Program
            {
                public void Main()
                {
                    var x = 1;
                    var z = 3;
                }
            }
            """.Replace("\r\n", "\n"));
    }

    [Test]
    public async Task ProcessAsync_WithNoConditionals_DoesNotAlterDocument()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            using System;

            public class Program
            {
                public void Main()
                {
                    var x = 1;
                    var y = 2;
                }
            }
            """);
        var processor = new PreprocessorConditionals();
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
    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
    public async Task ProcessAsync_WithExpressionConditionals_ShouldAlterDocument()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            using System;

            public class Program
            {
                public void Main()
                {
                    #if DEBUG && (TEST || TEST2)
                    var x = 1;
                    #endif
                    var y = 2;
                }
            }
            """);
        var processor = new PreprocessorConditionals();
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
            ).WithAdditionalPreprocessorMacros(new[] { "DEBUG", "TEST2" })
            .Close();

        
        // Act
        var result = await processor.ProcessAsync(document, metadata);

        // Assert
        var text = await result.GetTextAsync();
        text.ToString().Replace("\r\n", "\n").Should().Be(
            """
            using System;

            public class Program
            {
                public void Main()
                {
                    var x = 1;
                    var y = 2;
                }
            }
            """.Replace("\r\n", "\n"));
    }
}