using System.Collections.Immutable;
using FakeItEasy;
using FluentAssertions;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;
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

                       """;

        // Act
        var result = await composer.ComposeAsync(document, context);

        // Assert
        var str = result.ToString();
        str.Should().Be(expected.Replace("\r\n", "\n"));
        str.Should().NotContain("\r\n");
    }
}