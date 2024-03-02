using System.Collections.Immutable;
using FakeItEasy;
using FluentAssertions;
using Mdk.CommandLine.IngameScript;
using Mdk.CommandLine.IngameScript.DefaultProcessors;
using Mdk.CommandLine.SharedApi;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptComposers;

[TestFixture]
public class ComposerTests
{
    [Test]
    public async Task ComposeAsync_WithContent_ReturnsDocumentAsString()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            public class SomeClass
            {
                public void SomeMethod()
                {
                    Console.WriteLine(""Hello, World!"");
                }
            }
            public class Program: MyGridProgram
            {
                public Program()
                {
                    Runtime.UpdateFrequency = UpdateFrequency.Update10;
                }
            
                public void Main(string argument, UpdateType updateSource)
                {
                    var someClass = new SomeClass();
                    someClass.SomeMethod();
                    var someOtherClass = new SomeOtherClass();
                    someOtherClass.SomeOtherMethod();
                }
            }
            class SomeOtherClass
            {
                public void SomeOtherMethod()
                {
                    Console.WriteLine(""Goodbye, World!"");
                }
            }

            """);

        var composer = new Composer();
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
        
        const string expected = """
                       public Program()
                       {
                           Runtime.UpdateFrequency = UpdateFrequency.Update10;
                       }

                       public void Main(string argument, UpdateType updateSource)
                       {
                           var someClass = new SomeClass();
                           someClass.SomeMethod();
                           var someOtherClass = new SomeOtherClass();
                           someOtherClass.SomeOtherMethod();
                       }
                       }
                       public class SomeClass
                       {
                           public void SomeMethod()
                           {
                               Console.WriteLine(""Hello, World!"");
                           }
                       }
                       class SomeOtherClass
                       {
                           public void SomeOtherMethod()
                           {
                               Console.WriteLine(""Goodbye, World!"");
                           }
                       }

                       """;
        var console = A.Fake<IConsole>();

        // Act
        var result = await composer.ComposeAsync(document, console, metadata);

        // Assert
        var str = result.ToString();
        str.Should().Be(expected.Replace("\r\n", "\n"));
        str.Should().NotContain("\r\n");
    }
}