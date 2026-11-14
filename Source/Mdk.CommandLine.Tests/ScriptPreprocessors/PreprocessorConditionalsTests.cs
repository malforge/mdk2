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
public class PreprocessorConditionalsTests : DocumentProcessorTests<PreprocessorConditionals>
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
                    var x = 1;
                    var y = 2;
                }
            }
            """.Replace("\r\n", "\n")));
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
                    var z = 3;
                }
            }
            """.Replace("\r\n", "\n")));
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            ImmutableHashSet<string>.Empty
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

        // Assert
        Assert.That(result, Is.SameAs(document));
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            ImmutableHashSet.Create("DEBUG", "TEST2")
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

    [TestCase("#if DEBUG // comment\nvar x = 1;\n#endif\nvar y = 2;\n#if DEBUG\nvar z = 3;\n#endif\n", "var y = 2;\n", TestName = "Comment after #if")]
    [TestCase("#if DEBUG\nvar x = 1;\n#endif // DEBUG\nvar y = 2;\n", "var y = 2;\n", TestName = "Comment after #endif")]
    [TestCase("#if DEBUG\nvar x = 1;\n#else // release\nvar y = 2;\n#endif\n", "var y = 2;\n", TestName = "Comment after #else")]
    [TestCase("#if true\nvar x = 1;\n#endif\n", "var x = 1;\n", TestName = "Literal true")]
    [TestCase("#if false\nvar x = 1;\n#endif\nvar y = 2;\n", "var y = 2;\n", TestName = "Literal false")]
    [TestCase("#if TEST == true\nvar x = 1;\n#endif\n#if TEST != true\nvar y = 2;\n#endif\n", "var x = 1;\n", TestName = "Equality operators")]
    [TestCase("#if DEBUG\nvar x = 1;\n#elif TEST\nvar y = 2;\n#else\nvar z = 3;\n#endif\n", "var y = 2;\n", TestName = "Elif branch")]
    [TestCase("#if TEST\n#if DEBUG\nvar x = 1;\n#else\nvar y = 2;\n#endif\n#endif\n", "var y = 2;\n", TestName = "Nested blocks")]
    [TestCase("#define LABEL\n#undef TEST\n#if LABEL && !TEST\nvar x = 1;\n#endif\n", "var x = 1;\n", TestName = "Define and undef")]
    [TestCase("var s = @\"\n#endif\n\";\n#if DEBUG\nvar x = 1;\n#endif\n", "var s = @\"\n#endif\n\";\n", TestName = "Directive text inside a string")]
    [TestCase("#region Things\nvar x = 1;\n#endregion\n", "#region Things\nvar x = 1;\n#endregion\n", TestName = "Regions kept")]
    [TestCase("#if DEBUG\n#region Things\nvar x = 1;\n#endregion\n#endif\nvar y = 2;\n", "var y = 2;\n", TestName = "Region in excluded branch")]
    [TestCase("#if DEBUG\n#pragma warning disable CS0168\n#endif\nvar y = 2;\n", "var y = 2;\n", TestName = "Pragma in excluded branch")]
    [TestCase("#if DEBUG\n#error Not for debug builds\n#endif\nvar y = 2;\n", "var y = 2;\n", TestName = "Error in excluded branch")]
    [TestCase("#if TEST\n#pragma warning disable CS0168\n#endif\n", "#pragma warning disable CS0168\n", TestName = "Pragma in included branch kept")]
    public async Task ProcessAsync_WithDirective_KeepsCompiledCodeOnly(string code, string expected)
    {
        var workspace = new AdhocWorkspace();
        var document = workspace.AddProject("TestProject", LanguageNames.CSharp).AddDocument("TestDocument", code);
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
            ImmutableHashSet.Create("TEST")
        );

        var result = await new PreprocessorConditionals().ProcessAsync(document, context);

        var text = await result.GetTextAsync();
        Assert.That(text.ToString(), Is.EqualTo(expected));
    }
}