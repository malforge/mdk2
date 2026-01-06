using System.Collections.Immutable;
using FakeItEasy;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;
using Mdk.CommandLine.Shared;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptPostProcessors;

[TestFixture]
public class TypeTrimmerTests : DocumentProcessorTests<TypeTrimmer>
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
            class Program : MyGridProgram
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

        // Assert
        // Write documents to string and compare them
        var expected = await document.GetTextAsync();
        var actual = await result.GetTextAsync();

        Assert.That(actual.ToString(), Is.EqualTo(expected.ToString()));
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
            
            class Program : MyGridProgram
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

        // Assert
        // Write documents to string and compare them
        var expected = await document.GetTextAsync();
        var actual = await result.GetTextAsync();

        Assert.That(actual.ToString(), Is.EqualTo(expected.ToString()));
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
            
            class Program : MyGridProgram
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

        // Assert
        // Write documents to string and compare them
        var expected = await document.GetTextAsync();
        var actual = await result.GetTextAsync();

        Assert.That(actual.ToString(), Is.EqualTo(expected.ToString()));
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
            
            class Program : MyGridProgram
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

        // Assert
        // Write documents to string and compare them
        var expected = await document.GetTextAsync();
        var actual = await result.GetTextAsync();

        Assert.That(actual.ToString(), Is.EqualTo(expected.ToString()));
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
            
            class Program : MyGridProgram
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

        // Assert
        // Write documents to string and compare them
        var expected = await document.GetTextAsync();
        var actual = await result.GetTextAsync();

        Assert.That(actual.ToString(), Is.EqualTo(expected.ToString()));
    }
    
    [Test]
    public async Task ProcessAsync_WhenReferencingStruct_RetainsType()
    {
        const string testCode =
            """
            struct D {}
            
            class Program : MyGridProgram
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

        // Assert
        // Write documents to string and compare them
        var expected = await document.GetTextAsync();
        var actual = await result.GetTextAsync();

        Assert.That(actual.ToString(), Is.EqualTo(expected.ToString()));
    }
    
    [Test]
    public async Task ProcessAsync_WhenReferencingClass_RetainsType()
    {
        const string testCode =
            """
            class A {}
            
            class Program : MyGridProgram
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

        // Assert
        // Write documents to string and compare them
        var expected = await document.GetTextAsync();
        var actual = await result.GetTextAsync();

        Assert.That(actual.ToString(), Is.EqualTo(expected.ToString()));
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
            
            class Program : MyGridProgram
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
            
            class Program : MyGridProgram
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

        // Assert
        // Write documents to string and compare them
        var expected = await project.AddDocument("TestDocument", expectedCode).GetTextAsync();
        var actual = await result.GetTextAsync();

        Assert.That(actual.ToString(), Is.EqualTo(expected.ToString()));
    }

    [Test]
    public async Task ProcessAsync_WhenUnusedField_RemovesFields()
    {
        const string testCode =
            """
            class Program : MyGridProgram
            {
                int _usedField;
                int _unusedField;

                static int UsedStaticField;
                static int UnusedStaticField;

                public void Call()
                {
                    _usedField = 1;
                    UsedStaticField = 2;
                }

                static void Main()
                {
                    var program = new Program();
                    program.Call();
                }
            }
            """;

        const string expectedCode =
            """
            class Program : MyGridProgram
            {
                int _usedField;

                static int UsedStaticField;

                public void Call()
                {
                    _usedField = 1;
                    UsedStaticField = 2;
                }

                static void Main()
                {
                    var program = new Program();
                    program.Call();
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

        // Assert
        var expected = await project.AddDocument("TestDocument", expectedCode).GetTextAsync();
        var actual = await result.GetTextAsync();

        Assert.That(actual.ToString(), Is.EqualTo(expected.ToString()));
    }

    [Test]
    public async Task ProcessAsync_WhenUnusedMethod_RemovesMethods()
    {
        const string testCode =
            """
            class MyGridProgram
            {
            }

            class Script : MyGridProgram
            {
                void Main() {}
                void Used() {}
                void Unused() {}

                static void UsedStatic() {}
                static void UnusedStatic() {}

                public void Call()
                {
                    Used();
                    UsedStatic();
                }
            }

            class Utility
            {
                void Main() {}
                void Used() {}
                void Unused() {}

                public void Call()
                {
                    Used();
                }
            }

            class Program : MyGridProgram
            {
                static void Main()
                {
                    var script = new Script();
                    script.Call();
                    var utility = new Utility();
                    utility.Call();
                }

            }
            """;

        const string expectedCode =
            """
            class MyGridProgram
            {
            }

            class Script : MyGridProgram
            {
                void Main() {}
                void Used() {}

                static void UsedStatic() {}

                public void Call()
                {
                    Used();
                    UsedStatic();
                }
            }

            class Utility
            {
                void Used() {}

                public void Call()
                {
                    Used();
                }
            }

            class Program : MyGridProgram
            {
                static void Main()
                {
                    var script = new Script();
                    script.Call();
                    var utility = new Utility();
                    utility.Call();
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

        // Assert
        var expected = await project.AddDocument("TestDocument", expectedCode).GetTextAsync();
        var actual = await result.GetTextAsync();

        Assert.That(actual.ToString(), Is.EqualTo(expected.ToString()));
    }

    [Test]
    public async Task ProcessAsync_WhenUnusedCallbacksOutsideMyGridProgram_RemovesMembers()
    {
        const string testCode =
            """
            class Utility
            {
                // These members should disappear, as this is not a MyGridProgram
                public Utility() {}
                void Main() {}
                void Save() {}
            }

            class Program : MyGridProgram
            {
                // These members should not disappear, as this is not a MyGridProgram
                public Program()
                {
                    _ = typeof(Utility);
                }
                void Main() {}
                void Save() {}
            }
            """;

        const string expectedCode =
            """
            class Utility
            {
            }

            class Program : MyGridProgram
            {
                // These members should not disappear, as this is not a MyGridProgram
                public Program()
                {
                    _ = typeof(Utility);
                }
                void Main() {}
                void Save() {}
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
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        // Act
        var result = await processor.ProcessAsync(document, context);

        // Assert
        var expected = await project.AddDocument("TestDocument", expectedCode).GetTextAsync();
        var actual = await result.GetTextAsync();

        Assert.That(actual.ToString(), Is.EqualTo(expected.ToString()));
    }
}
