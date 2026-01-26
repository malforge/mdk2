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
    public async Task ProcessAsync_WhenMinifyNone_DoesNotModifyDocument()
    {
        const string testCode =
            """
            /// <summary>Unused type docs.</summary>
            class UnusedType
            {
                /// <summary>Unused method docs.</summary>
                public void NeverCalled()
                {
                }
            }

            class Program : MyGridProgram
            {
                /// <summary>Unused helper docs.</summary>
                void Helper()
                {
                    var unused = 1;
                }

                void Main()
                {
                    Echo("hi");
                }
            }
            """;

        // Arrange
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", testCode);
        var processor = new TypeTrimmer();
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
        var expected = await document.GetTextAsync();
        var actual = await result.GetTextAsync();

        Assert.That(actual.ToString(), Is.EqualTo(expected.ToString()));
    }

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
        // Test
        // - Used+Unused types like Classes, Structs, Interfaces, Enums, Delegates, Fields, Properties
        // - Used+Unused nested types, as above - in a used container type
        // - Special case of a type with a nested type, both unused
        const string testCode =
            """
            class UsedClass {}
            class UnusedClass {}
            struct UsedStruct {}
            struct UnusedStruct {}
            interface UsedInterface {}
            interface UnusedInterface {}
            enum UsedEnum { A }
            enum UnusedEnum { A }
            delegate void UsedDelegate();
            delegate void UnusedDelegate();
            class UnusedContainer
            {
                class UnusedNestedClass {}
            }
            class UsedContainer
            {
                public class UsedNestedClass {}
                public class UnusedNestedClass {}
                public struct UsedNestedStruct {}
                public struct UnusedNestedStruct {}
                public interface UsedNestedInterface {}
                public interface UnusedNestedInterface {}
                public enum UsedNestedEnum { A }
                public enum UnusedNestedEnum { A }
                public delegate void UsedNestedDelegate();
                public delegate void UnusedNestedDelegate();
                public int UsedField;
                public int UnusedField;
                public int UsedProperty { get; set; }
                public int UnusedProperty { get; set; }
                public static int UsedStaticField;
                public static int UnusedStaticField;
                public void UsedMethod() {}
                public void UnusedMethod() {}
                public static void UsedStaticMethod() {}
                public static void UnusedStaticMethod() {}
            }
            class Program : MyGridProgram
            {
                static void Main()
                {
                    var usedContainer = new UsedContainer();
                    var usedClass = new UsedClass();
                    var usedStruct = new UsedStruct();
                    UsedInterface usedInterface = null;
                    var usedEnum = UsedEnum.A;
                    UsedDelegate usedDelegate = null;
                    var usedNestedClass = new UsedContainer.UsedNestedClass();
                    var usedNestedStruct = new UsedContainer.UsedNestedStruct();
                    UsedContainer.UsedNestedInterface usedNestedInterface = null;
                    var usedNestedEnum = UsedContainer.UsedNestedEnum.A;
                    UsedContainer.UsedNestedDelegate usedNestedDelegate = null;

                    usedContainer.UsedField = 1;
                    usedContainer.UsedProperty = 3;
                    UsedContainer.UsedStaticField = 2;
                    usedContainer.UsedMethod();
                    UsedContainer.UsedStaticMethod();
                }
            }
            """;

        const string expectedCode =
            """
            class UsedClass {}
            struct UsedStruct {}
            interface UsedInterface {}
            enum UsedEnum { A }
            delegate void UsedDelegate();
            class UsedContainer
            {
                public class UsedNestedClass {}
                public struct UsedNestedStruct {}
                public interface UsedNestedInterface {}
                public enum UsedNestedEnum { A }
                public delegate void UsedNestedDelegate();
                public int UsedField;
                public int UsedProperty { get; set; }
                public static int UsedStaticField;
                public void UsedMethod() {}
                public static void UsedStaticMethod() {}
            }
            class Program : MyGridProgram
            {
                static void Main()
                {
                    var usedContainer = new UsedContainer();
                    var usedClass = new UsedClass();
                    var usedStruct = new UsedStruct();
                    UsedInterface usedInterface = null;
                    var usedEnum = UsedEnum.A;
                    UsedDelegate usedDelegate = null;
                    var usedNestedClass = new UsedContainer.UsedNestedClass();
                    var usedNestedStruct = new UsedContainer.UsedNestedStruct();
                    UsedContainer.UsedNestedInterface usedNestedInterface = null;
                    var usedNestedEnum = UsedContainer.UsedNestedEnum.A;
                    UsedContainer.UsedNestedDelegate usedNestedDelegate = null;

                    usedContainer.UsedField = 1;
                    usedContainer.UsedProperty = 3;
                    UsedContainer.UsedStaticField = 2;
                    usedContainer.UsedMethod();
                    UsedContainer.UsedStaticMethod();
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
    public async Task ProcessAsync_WhenFieldDeclarationHasMixedUsage_RemovesUnusedVariables()
    {
        const string testCode =
            """
            class Container
            {
                int UsedField = 1, UnusedField = 2;
            }

            class Program : MyGridProgram
            {
                void Main()
                {
                    var container = new Container();
                    _ = container.UsedField;
                }
            }
            """;

        const string expectedCode =
            """
            class Container
            {
                int UsedField = 1;
            }

            class Program : MyGridProgram
            {
                void Main()
                {
                    var container = new Container();
                    _ = container.UsedField;
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
    public async Task ProcessAsync_WhenRecursiveTypeUsage_RemovesUnusedChain()
    {
        const string testCode =
            """
            class A1 {}
            class B2 { public A1 Value; }
            class B3 { public B2 Value; }
            class C2 { public A1 Value; }
            class C3 { public C2 Value; }
            class Program : MyGridProgram
            {
                static void Main()
                {
                    var c3 = new C3();
                    _ = c3.Value.Value;
                }
            }
            """;

        const string expectedCode =
            """
            class A1 {}
            class C2 { public A1 Value; }
            class C3 { public C2 Value; }
            class Program : MyGridProgram
            {
                static void Main()
                {
                    var c3 = new C3();
                    _ = c3.Value.Value;
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
    public async Task ProcessAsync_WhenNoMemberTrimming_RemovesUnusedTypesOnly()
    {
        const string testCode =
            """
            class UnusedType {}
            class UnusedTypeWithMembers
            {
                int UnusedField;
                int UnusedProperty { get; set; }
                void UnusedMethod() {}
                delegate void UnusedDelegate();
            }
            class Program : MyGridProgram
            {
                static void Main()
                {
                    _ = new UnusedTypeWithMembers();
                }
            }
            """;

        const string expectedCode =
            """
            class UnusedTypeWithMembers
            {
                int UnusedField;
                int UnusedProperty { get; set; }
                void UnusedMethod() {}
                delegate void UnusedDelegate();
            }
            class Program : MyGridProgram
            {
                static void Main()
                {
                    _ = new UnusedTypeWithMembers();
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
                MinifierExtraOptions = MinifierExtraOptions.NoMemberTrimming,
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
    public async Task ProcessAsync_WhenAbstractMemberIsOverridden_PreservesBaseMember()
    {
        const string testCode =
            """
            abstract class Base
            {
                // Abstract member must remain when derived types override it.
                public abstract void Tick();
            }

            class Derived : Base
            {
                public override void Tick()
                {
                }
            }

            class Program : MyGridProgram
            {
                void Main()
                {
                    _ = new Derived();
                }
            }
            """;

        // Arrange
        using var workspace = new AdhocWorkspace();
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
        var expected = await document.GetTextAsync();
        var actual = await result.GetTextAsync();

        Assert.That(actual.ToString(), Is.EqualTo(expected.ToString()));
    }

    [Test]
    public async Task ProcessAsync_WhenBaseConstructorHasSideEffects_PreservesConstructor()
    {
        const string testCode =
            """
            class Base
            {
                // Base constructor is implicitly called by derived types and must remain.
                public Base()
                {
                    Program.BaseConstructed = true;
                }
            }

            class Derived : Base
            {
            }

            class Program : MyGridProgram
            {
                public static bool BaseConstructed;

                void Main()
                {
                    _ = new Derived();
                }
            }
            """;

        // Arrange
        using var workspace = new AdhocWorkspace();
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
        var expected = await document.GetTextAsync();
        var actual = await result.GetTextAsync();

        Assert.That(actual.ToString(), Is.EqualTo(expected.ToString()));
    }

    [Test]
    public async Task ProcessAsync_WhenTypeUsedWithNewConstraint_PreservesDefaultConstructor()
    {
        const string testCode =
            """
            class A
            {
                // new() constraints require a public parameterless ctor to remain.
                public A() {}
                public A(int value) {}
            }

            class B<T> where T : new()
            {
                public T Create()
                {
                    return new T();
                }
            }

            class Program : MyGridProgram
            {
                void Main()
                {
                    var factory = new B<A>();
                    _ = factory.Create();
                    _ = new A(1);
                }
            }
            """;

        // Arrange
        using var workspace = new AdhocWorkspace();
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
        var expected = await document.GetTextAsync();
        var actual = await result.GetTextAsync();

        Assert.That(actual.ToString(), Is.EqualTo(expected.ToString()));
    }

    [Test]
    public async Task ProcessAsync_WhenStaticFieldInitializerHasSideEffects_PreservesField()
    {
        const string testCode =
            """
            class Registrar
            {
                // Static and instance initializers with methods have side effects even if the fields are never read.
                static readonly int _static = RegisterStatic();
                readonly int _instance = RegisterInstance();
                static int StaticProperty { get; } = RegisterStatic();
                int InstanceProperty { get; } = RegisterInstance();
                
                // Chained assignments can also do side effects
                static int _usedStaticFieldChained;
                static int _unusedStaticFieldChained = _usedStaticFieldChained = 1;
                
                // Static and instance initializers without side effects should be removed.
                static int StaticZeroField = 0;
                int InstanceZeroField = 0;
                static int StaticZeroProperty { get; } = 0;
                int InstanceZeroProperty { get; } = 0;

                static int RegisterStatic()
                {
                    _ = _usedStaticFieldChained;
                    Program.RegisterCount++;
                    return 0;
                }

                int RegisterInstance()
                {
                    Program.RegisterCount++;
                    return 0;
                }
            }

            class Program : MyGridProgram
            {
                public static int RegisterCount;

                void Main()
                {
                    _ = typeof(Registrar);
                    _ = new Registrar();
                }
            }
            """;

        const string expectedCode =
            """
            class Registrar
            {
                // Static and instance initializers with methods have side effects even if the fields are never read.
                static readonly int _static = RegisterStatic();
                readonly int _instance = RegisterInstance();
                static int StaticProperty { get; } = RegisterStatic();
                int InstanceProperty { get; } = RegisterInstance();
                
                // Chained assignments can also do side effects
                static int _usedStaticFieldChained;
                static int _unusedStaticFieldChained = _usedStaticFieldChained = 1;

                static int RegisterStatic()
                {
                    _ = _usedStaticFieldChained;
                    Program.RegisterCount++;
                    return 0;
                }

                int RegisterInstance()
                {
                    Program.RegisterCount++;
                    return 0;
                }
            }

            class Program : MyGridProgram
            {
                public static int RegisterCount;

                void Main()
                {
                    _ = typeof(Registrar);
                    _ = new Registrar();
                }
            }
            """;

        // Arrange
        using var workspace = new AdhocWorkspace();
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
