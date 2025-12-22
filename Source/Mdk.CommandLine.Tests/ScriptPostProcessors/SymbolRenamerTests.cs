using System.Collections.Immutable;
using FakeItEasy;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptPostProcessors;

[TestFixture]
public class SymbolRenamerTests : DocumentProcessorTests<SymbolRenamer>
{
    [Test]
    public async Task ProcessAsync_WhenForEachUsesQualifiedType_ProducesExpectedMinifiedOutput()
    {
        const string testCode =
            """
            class Program
            {
                void Test()
                {
                    // Replacing Program.Inner with var would infer "object" and change the element type.
                    foreach (Program.Inner item in Program.Matches())
                    {
                        var group = item.Groups;
                    }
                }

                public class Inner
                {
                    public int Groups;
                }

                public static System.Collections.IEnumerable Matches()
                {
                    yield return new Inner();
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
        var forEach = syntaxRoot!.DescendantNodes().OfType<ForEachStatementSyntax>().Single();

        Assert.That(forEach.Type, Is.TypeOf<QualifiedNameSyntax>());
    }

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