using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using FakeItEasy;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;
using Mdk.CommandLine.Shared;
using Mdk.CommandLine.Shared.Api;
using Mdk.CommandLine.Shared.DefaultProcessors;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptPreprocessors;

[TestFixture]
[SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
public class PreprocessorDefineSymbolTests : DocumentProcessorTests<PreprocessorConditionals>
{
    //function_withthisstate_shoulddothis
    [Test]
    public async Task ProcessAsync_WithSimpleWithDefineLABEL1withIfDEBUG_RemovesDebugBlock()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            #define LABEL1
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            ImmutableHashSet.Create<string>()
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

        // Assert
        var text = await result.GetTextAsync();
        //Console.WriteLine(text);
        Assert.That(text.ToString().Replace("\r\n", "\n"), Is.EqualTo(
            """
            using System;

            public class Program
            {
                public void Main()
                {
                    var y = 2;
                }
            }
            """.Replace("\r\n", "\n")));
    }

    [Test]
    public async Task ProcessAsync_WithDEBUGconfigAndIfLABEL1_ShouldRemoveBlock()
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
                    #if LABEL1
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            ImmutableHashSet.Create("DEBUG")
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

        // Assert
        var text = await result.GetTextAsync();
        Assert.That(text.ToString().Replace("\r\n", "\n"), Is.EqualTo(
            """
            using System;

            public class Program
            {
                public void Main()
                {
                    var y = 2;
                }
            }
            """.Replace("\r\n", "\n")));
    }



    [Test]
    public async Task ProcessAsync_WithLABEL1asConfigWithLABEL1block_ShouldLeaveBlock()
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
                    #if LABEL1
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            ImmutableHashSet.Create("LABEL1")
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

        // Assert
        var text = await result.GetTextAsync();
        Assert.That(text.ToString().Replace("\r\n", "\n"), Is.EqualTo(
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
            """.Replace("\r\n", "\n")));
    }


    [Test]
    public async Task ProcessAsync_WithoutLABEL1config_WithLABEL1block_ShouldRemoveBlock()
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
                    #if LABEL1
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            ImmutableHashSet.Create<string>()
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

        // Assert
        var text = await result.GetTextAsync();
        Assert.That(text.ToString().Replace("\r\n", "\n"), Is.EqualTo(
            """
            using System;

            public class Program
            {
                public void Main()
                {
                    var y = 2;
                }
            }
            """.Replace("\r\n", "\n")));
    }


    [Test]
    public async Task ProcessAsync_withDefineLABEL1withLABEL1block_ShouldLeaveBlock()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            #define LABEL1
            using System;

            public class Program
            {
                public void Main()
                {
                    #if LABEL1
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            ImmutableHashSet.Create<string>()
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

        // Assert
        var text = await result.GetTextAsync();
        Assert.That(text.ToString().Replace("\r\n", "\n"), Is.EqualTo(
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
            """.Replace("\r\n", "\n")));
    }


    [Test]
    public async Task ProcessAsync_withDefineLABEL2withLABEL1block_ShouldRemoveBlock()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            #define LABEL2
            using System;

            public class Program
            {
                public void Main()
                {
                    #if LABEL1
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            ImmutableHashSet.Create<string>()
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

        // Assert
        var text = await result.GetTextAsync();
        Assert.That(text.ToString().Replace("\r\n", "\n"), Is.EqualTo(
            """
            using System;

            public class Program
            {
                public void Main()
                {
                    var y = 2;
                }
            }
            """.Replace("\r\n", "\n")));
    }


    [Test]
    public async Task ProcessAsync_withDefineLABEL1and2withLABEL1blockandLABEL2Block_ShouldLeaveBlocks()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            #define LABEL1
            #define LABEL2
            using System;

            public class Program
            {
                public void Main()
                {
                    #if LABEL1
                    var x = 1;
                    #endif
                    #if LABEL2
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            ImmutableHashSet.Create<string>()
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

        // Assert
        var text = await result.GetTextAsync();
        Assert.That(text.ToString().Replace("\r\n", "\n"), Is.EqualTo(
            """
            using System;

            public class Program
            {
                public void Main()
                {
                    var x = 1;
                    var y = 2;
                    var z = 3;
                }
            }
            """.Replace("\r\n", "\n")));
    }

}