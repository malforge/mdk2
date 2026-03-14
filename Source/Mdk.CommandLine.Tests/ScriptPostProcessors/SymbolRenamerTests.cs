using System.Collections.Immutable;
using FakeItEasy;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;
using Mdk.CommandLine.Shared;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptPostProcessors;

[TestFixture]
public class SymbolRenamerTests : DocumentProcessorTests<SymbolRenamer>
{
    [Test]
    public async Task ProcessAsync_WhenQueryRangeVariableIsRenamed_DeclarationMatchesUsage()
    {
        const string testCode =
            """
            class Program
            {
                void Test()
                {
                    var items = new[] { "a", "b" };
                    // "item" must be renamed consistently in both declaration and usage sites.
                    var query =
                        from item in items
                        orderby item
                        select item;
                }
            }
            """;

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", testCode);
        var processor = new SymbolRenamer();
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
            A.Fake<IConsole>(),
            A.Fake<IInteraction>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        var result = await processor.ProcessAsync(document, context);
        var syntaxRoot = await result.GetSyntaxRootAsync();
        var query = syntaxRoot!.DescendantNodes().OfType<QueryExpressionSyntax>().Single();
        var fromIdentifier = query.FromClause.Identifier.ValueText;
        var selectClause = (SelectClauseSyntax)query.Body.SelectOrGroup;
        var selectIdentifier = ((IdentifierNameSyntax)selectClause.Expression).Identifier.ValueText;
        var orderByClause = query.Body.Clauses.OfType<OrderByClauseSyntax>().Single();
        var orderByIdentifier = ((IdentifierNameSyntax)orderByClause.Orderings.Single().Expression).Identifier.ValueText;

        Assert.That(selectIdentifier, Is.EqualTo(fromIdentifier));
        Assert.That(orderByIdentifier, Is.EqualTo(fromIdentifier));
    }

    [Test]
    public async Task ProcessAsync_WhenSymbolProtectionAnnotatesProgram_KeepsProtectedNames()
    {
        const string testCode =
            """
            class Program
            {
                public Program()
                {
                }

                public void Main()
                {
                    Helper();
                }

                public void Save()
                {
                }

                void Helper()
                {
                }
            }
            """;

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", testCode);
        var annotator = new SymbolProtectionAnnotator();
        var processor = new SymbolRenamer();
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
            A.Fake<IConsole>(),
            A.Fake<IInteraction>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        var protectedDocument = await annotator.ProcessAsync(document, context);
        var result = await processor.ProcessAsync(protectedDocument, context);
        var root = await result.GetSyntaxRootAsync();

        Assert.That(root, Is.Not.Null);
        var programClass = root!.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
        var constructor = programClass.Members.OfType<ConstructorDeclarationSyntax>().Single();
        var methods = programClass.Members.OfType<MethodDeclarationSyntax>().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(programClass.Identifier.ValueText, Is.EqualTo("Program"));
            Assert.That(constructor.Identifier.ValueText, Is.EqualTo("Program"));
            Assert.That(methods.Single(m => m.Identifier.ValueText == "Main"), Is.Not.Null);
            Assert.That(methods.Single(m => m.Identifier.ValueText == "Save"), Is.Not.Null);
            Assert.That(methods.Any(m => m.Identifier.ValueText == "Helper"), Is.False);
        });
    }

    [Test]
    public async Task ProcessAsync_WithPreservedAndUnpreservedEnums_SharedMemberNames_OnlyKeepsPreservedOne()
    {
        const string testCode =
            """
            namespace TestNamespace
            {
                #region mdk preserve
                enum PreservedEnum
                {
                    Alpha
                }
                #endregion

                enum OtherEnum
                {
                    Alpha
                }

                class Program
                {
                    void Test()
                    {
                        var a = PreservedEnum.Alpha;
                        var b = OtherEnum.Alpha;
                    }
                }
            }
            """;

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", testCode);
        var deleteNamespaces = new DeleteNamespaces();
        var processor = new SymbolRenamer();
        var context = CreateContext();
        var flattenedDocument = await deleteNamespaces.ProcessAsync(document, context);
        var result = await processor.ProcessAsync(flattenedDocument, context);
        var resultRoot = await result.GetSyntaxRootAsync();
        var enums = resultRoot!.DescendantNodes().OfType<EnumDeclarationSyntax>().ToArray();
        var preservedResult = enums.Single(e => e.Identifier.ValueText == "PreservedEnum");
        var otherResult = enums.Single(e => e.Identifier.ValueText != "PreservedEnum");

        Assert.Multiple(() =>
        {
            Assert.That(preservedResult.Members.Single().Identifier.ValueText, Is.EqualTo("Alpha"));
            Assert.That(otherResult.Identifier.ValueText, Is.Not.EqualTo("OtherEnum"));
            Assert.That(otherResult.Members.Single().Identifier.ValueText, Is.Not.EqualTo("Alpha"));
        });
    }

    static PackContext CreateContext()
    {
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

        return new PackContext(
            parameters,
            A.Fake<IConsole>(),
            A.Fake<IInteraction>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );
    }

//     [Test]
//     public async Task Regression__InheritedSymbolDidNotRename()
//     {
//         const string testCode =
//             """
//             abstract class BaseClass
//             {
//                 public override string ToString() => "BaseClass";
//             }
//             class Derived1 : BaseClass
//             {
//                 public override string ToString() => "Derived1";
//             }
//
//             class Program
//             {
//                 static void Main()
//                 {
//                     var a = new Derived1();
//                 }
//             }
//             """;
//         
//         const string expectedCode =
//             """
//             abstract class B
//             {
//                 public override string ToString() => "BaseClass";
//             }
//             class C : B
//             {
//                 public override string ToString() => "Derived1";
//             }
//             
//             class F
//             {
//                 static void E()
//                 {
//                     var D = new C();
//                 }
//             }
//             """;
//
//         // Arrange
//         var workspace = new AdhocWorkspace();
//         var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
//         var document = project.AddDocument("TestDocument", testCode);
//         var preprocessor = new SymbolProtectionAnnotator();
//         var processor = new SymbolRenamer();
//         var parameters = new Parameters
//         {
//             Verb = Verb.Pack,
//             PackVerb =
//             {
//                 MinifierLevel = MinifierLevel.Full,
//                 ProjectFile = @"A:\Fake\Path\Project.csproj",
//                 Output = @"A:\Fake\Path\Output"
//             }
//         };
//         var context = new PackContext(
//             parameters,
//             A.Fake<IConsole>(),
//             A.Fake<IInteraction>(o => o.Strict()),
//             A.Fake<IFileFilter>(o => o.Strict()),
//             A.Fake<IFileSystem>(),
//             A.Fake<IImmutableSet<string>>(o => o.Strict())
//         );
//
//         // Act
//         var preprocessed = await preprocessor.ProcessAsync(document, context);
//         var result = await processor.ProcessAsync(preprocessed, context);
//
//         // Assert
//         // Write documents to string and compare them
//         var actual = await result.GetTextAsync();
//
//         actual.ToString().Should().Be(expectedCode);
//     }
}
