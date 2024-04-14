using System.Collections.Immutable;
using FakeItEasy;
using FluentAssertions;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;
using Mdk.CommandLine.SharedApi;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptPostProcessors;

[TestFixture]
public class TypeTrimmerTests : ScriptPostProcessorTests<TypeTrimmer>
{
    [Test]
    public async Task ProcessAsync_WhenAllTypesAreUsed_ReturnsDocument()
    {
        const string testCode =
            """
            class A {}
            class B {}
            class C {}
            struct D {}
            interface I {}
            enum E 
            {
                A,
                B,
                C
            }
            
            class X: I
            {
            }
            class Program
            {
                static void Main()
                {
                    var a = new A();
                    var b = new B();
                    var c = new C();
                    var d = new D();
                    var x = new X();
                    var e = E.A;                    
                }
            }
            """;

        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", testCode);
        var processor = new TypeTrimmer();
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb =
            {
                MinifierLevel = MinifierLevel.Trim,
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
        // Write documents to string and compare them
        var expected = await document.GetTextAsync();
        var actual = await result.GetTextAsync();

        actual.ToString().Should().Be(expected.ToString());
    }
    
    [Test]
    public async Task ProcessAsync_WhenReferencingStaticMember_RetainsType()
    {
        const string testCode =
            """
            class A
            {
                public static void Method() {}
            }
            
            class Program
            {
                static void Main()
                {
                    A.Method();
                }
            }
            """;

        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", testCode);
        var processor = new TypeTrimmer();
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb =
            {
                MinifierLevel = MinifierLevel.Trim,
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
        // Write documents to string and compare them
        var expected = await document.GetTextAsync();
        var actual = await result.GetTextAsync();

        actual.ToString().Should().Be(expected.ToString());
    }
    
    [Test]
    public async Task ProcessAsync_WhenReferencingExtensionMethod_RetainsType()
    {
        const string testCode =
            """
            static class Extensions
            {
                public static void Method(this A a) {}
            }
            
            class A {}
            
            class Program
            {
                static void Main()
                {
                    var a = new A();
                    a.Method();
                }
            }
            """;

        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", testCode);
        var processor = new TypeTrimmer();
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb =
            {
                MinifierLevel = MinifierLevel.Trim,
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
        // Write documents to string and compare them
        var expected = await document.GetTextAsync();
        var actual = await result.GetTextAsync();

        actual.ToString().Should().Be(expected.ToString());
    }
    
    [Test]
    public async Task ProcessAsync_WhenReferencingEnum_RetainsType()
    {
        const string testCode =
            """
            enum E
            {
                A,
                B,
                C
            }
            
            class Program
            {
                static void Main()
                {
                    var e = E.A;
                }
            }
            """;

        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", testCode);
        var processor = new TypeTrimmer();
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb =
            {
                MinifierLevel = MinifierLevel.Trim,
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
        // Write documents to string and compare them
        var expected = await document.GetTextAsync();
        var actual = await result.GetTextAsync();

        actual.ToString().Should().Be(expected.ToString());
    }
    
    [Test]
    public async Task ProcessAsync_WhenReferencingInterface_RetainsType()
    {
        const string testCode =
            """
            interface I {}
            
            class X: I
            {
            }
            
            class Program
            {
                static void Main()
                {
                    var x = new X();
                }
            }
            """;

        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", testCode);
        var processor = new TypeTrimmer();
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb =
            {
                MinifierLevel = MinifierLevel.Trim,
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
        // Write documents to string and compare them
        var expected = await document.GetTextAsync();
        var actual = await result.GetTextAsync();

        actual.ToString().Should().Be(expected.ToString());
    }
    
    [Test]
    public async Task ProcessAsync_WhenReferencingStruct_RetainsType()
    {
        const string testCode =
            """
            struct D {}
            
            class Program
            {
                static void Main()
                {
                    var d = new D();
                }
            }
            """;

        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", testCode);
        var processor = new TypeTrimmer();
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb =
            {
                MinifierLevel = MinifierLevel.Trim,
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
        // Write documents to string and compare them
        var expected = await document.GetTextAsync();
        var actual = await result.GetTextAsync();

        actual.ToString().Should().Be(expected.ToString());
    }
    
    [Test]
    public async Task ProcessAsync_WhenReferencingClass_RetainsType()
    {
        const string testCode =
            """
            class A {}
            
            class Program
            {
                static void Main()
                {
                    var a = new A();
                }
            }
            """;

        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", testCode);
        var processor = new TypeTrimmer();
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb =
            {
                MinifierLevel = MinifierLevel.Trim,
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
        // Write documents to string and compare them
        var expected = await document.GetTextAsync();
        var actual = await result.GetTextAsync();

        actual.ToString().Should().Be(expected.ToString());
    }

    [Test]
    public async Task ProcessAsync_WhenUnusedType_ReturnsDocumentWithoutType()
    {
        const string testCode =
            """
            class A {}
            class B {}
            class C {}
            struct D {}
            interface I {}
            enum E 
            {
                A,
                B,
                C
            }
            
            class Program
            {
                static void Main()
                {
                    var a = new A();
                    var b = new B();
                    var c = new C();
                    var d = new D();
                    var e = E.A;                    
                }
            }
            """;

        const string expectedCode =
            """
            class A {}
            class B {}
            class C {}
            struct D {}
            enum E 
            {
                A,
                B,
                C
            }
            
            class Program
            {
                static void Main()
                {
                    var a = new A();
                    var b = new B();
                    var c = new C();
                    var d = new D();
                    var e = E.A;                    
                }
            }
            """;

        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", testCode);
        var processor = new TypeTrimmer();
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb =
            {
                MinifierLevel = MinifierLevel.Trim,
                ProjectFile = @"A:\Fake\Path\Project.csproj",
                Output = @"A:\Fake\Path\Output"
            }
        };
        var context = new PackContext(
            parameters,
            A.Fake<IConsole>(),
            A.Fake<IInteraction>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

        // Assert
        // Write documents to string and compare them
        var expected = await project.AddDocument("TestDocument", expectedCode).GetTextAsync();
        var actual = await result.GetTextAsync();

        actual.ToString().Should().Be(expected.ToString());
    }
}