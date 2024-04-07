using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using FakeItEasy;
using FluentAssertions;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;
using Mdk.CommandLine.SharedApi;
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
            ImmutableHashSet.Create<string>()
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

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
            ImmutableHashSet.Create("DEBUG")
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

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
            ImmutableHashSet.Create<string>()
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

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
            ImmutableHashSet.Create<string>()
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

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
            ImmutableHashSet.Create<string>()
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

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
        var result = await processor.ProcessAsync(document, context);

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
            ImmutableHashSet.Create("DEBUG", "TEST2")
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

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