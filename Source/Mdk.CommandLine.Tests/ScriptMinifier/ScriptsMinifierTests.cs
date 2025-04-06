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

namespace MDK.CommandLine.Tests.MinifierSubsystemsTests;

[TestFixture]
[SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
public class MinifierSubsystemsTests
{
    //==================================

    [Test]
    public async Task WhitespaceTrimmer_WhitespaceWithLabels_ShouldRemove()
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
        var processor = new WhitespaceTrimmer();
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb =
            {
                MinifierLevel = MinifierLevel.Full,
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
        Console.WriteLine("full minifier is on:" + text + " does it look minified ?");
        text.ToString().Replace("\r\n", "\n").Should().Be(
            """
            using System;

            public class Program
            {
            public void Main(){var y=2;}}
            """.Replace("\r\n", "\n"));
    }

    //==================



    [Test]
    public async Task WhitespaceTrimmer_whitespaceMinusWhitespaceMinusWhitespace_ShouldBeMinusWhitespaceMinus()
    {
        string startingDocument =
            """
            using System;

            public class Program
            {
                public void Main()
                {
                    var y = 2;
                    int z = 2 - - 1;
                }
            }
            """;

        string expectedDocument =
            """
            using System;

            public class Program
            {
            public void Main(){var y=2;int z=2- -1;}}
            """;

        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", startingDocument);
        var processor = new WhitespaceTrimmer();
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb =
            {
                MinifierLevel = MinifierLevel.Full,
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
        Console.WriteLine("full minifier is on:" + text + " does it look minified ?");
        text.ToString().Replace("\r\n", "\n").Should().Be(expectedDocument.Replace("\r\n", "\n"));
    }

    //==================



    [Test]
    public async Task WhitespaceTrimmer_whitespacePreDecrementWithDebugLabel_ShouldBeMinusWhitespaceMinus()
    {
        string startingDocument =
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
                    int z = 2 -- 1;
                }
            }
            """;

        string expectedDocument =
            """
            using System;

            public class Program
            {
            public void Main(){var y=2;int z=2--1;}}
            """;

        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", startingDocument);
        var processor = new WhitespaceTrimmer();
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb =
            {
                MinifierLevel = MinifierLevel.Full,
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
        Console.WriteLine("full minifier is on:" + text + " does it look minified ?");
        text.ToString().Replace("\r\n", "\n").Should().Be(expectedDocument.Replace("\r\n", "\n"));
    }

    [Test]
    public async Task WhitespaceTrimmer_whitespacePreDecrement_ShouldBeMinusWhitespaceMinus()
    {
        string startingDocument =
            """
            using System;

            public class Program
            {
                public void Main()
                {
                    var y = 2;
                    int z = 2 -- 1;
                }
            }
            """;

        string expectedDocument =
            """
            using System;

            public class Program
            {
            public void Main(){var y=2;int z=2--1;}}
            """;

        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", startingDocument);
        var processor = new WhitespaceTrimmer();
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb =
            {
                MinifierLevel = MinifierLevel.Full,
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
        Console.WriteLine("full minifier is on:" + text + " does it look minified ?");
        text.ToString().Replace("\r\n", "\n").Should().Be(expectedDocument.Replace("\r\n", "\n"));
    }


}