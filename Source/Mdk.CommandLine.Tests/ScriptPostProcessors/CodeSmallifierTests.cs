using System.Collections.Immutable;
using FakeItEasy;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;
using Mdk.CommandLine.Shared;
using Mdk.CommandLine.Shared.Api;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptPostProcessors;

[TestFixture]
public class CodeSmallifierTests : DocumentProcessorTests<CodeSmallifier>
{
    [Test]
    public async Task ProcessAsync_WhenFieldsAreUninitialized_CompactsByType()
    {
        const string testCode =
            """
            class Program
            {
                private static string A;
                private static string B = "x";
                internal string C;
                string[] D;
                string E;
                int F;
                private int G = 1;
                string H;
            }
            """;

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", testCode);
        var processor = new CodeSmallifier();
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb =
            {
                MinifierLevel = MinifierLevel.Lite,
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
        var actual = await result.GetTextAsync();
            var expected =
            """
            class Program
            {
                static string A,B = "x";
                internal string C;
                string[] D;
                string E,H;
                int F,G = 1;
            }
            """;

        Assert.That(actual.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_WhenFieldsAreUninitialized_CompactsByTypeSecondaryOrder()
    {
        const string testCode =
            """
            class Program
            {
                private static string A = "x";
                private static string B;
                internal string C;
                string[] D;
                string E;
                int F = 1;
                private int G;
                string H;
            }
            """;

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", testCode);
        var processor = new CodeSmallifier();
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb =
            {
                MinifierLevel = MinifierLevel.Lite,
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
        var actual = await result.GetTextAsync();
            var expected =
            """
            class Program
            {
                static string A = "x",B;
                internal string C;
                string[] D;
                string E,H;
                int F = 1,G;
            }
            """;

        Assert.That(actual.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_WhenFieldsArePreserved_DoesNotCompactThem()
    {
        // Regression test for issue #149: fields inside a "mdk preserve" region must stay exactly
        // as written, and a non-preserved field outside the region must not be folded in with them.
        const string testCode =
            """
            class Program
            {
                #region mdk preserve
                const string ControlSeat = "Flight Seat Alpha";
                const string CockpitName = "Main Cockpit";
                #endregion
                const string InternalTag = "internal";
            }
            """;

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", testCode);
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

        // RegionAnnotator applies the "mdk preserve" annotation the CodeSmallifier must respect.
        var annotated = await new RegionAnnotator().ProcessAsync(document, context);
        var result = await new CodeSmallifier().ProcessAsync(annotated, context);
        var root = await result.GetSyntaxRootAsync();
        var fields = root!.DescendantNodes().OfType<FieldDeclarationSyntax>().ToArray();

        // Each declaration must remain on its own: the two preserved constants stay exactly as
        // written, and the unpreserved InternalTag is not folded in with them.
        Assert.Multiple(() =>
        {
            Assert.That(fields, Has.Length.EqualTo(3));
            Assert.That(fields.All(f => f.Declaration.Variables.Count == 1), Is.True);
            var names = fields.SelectMany(f => f.Declaration.Variables).Select(v => v.Identifier.ValueText).ToArray();
            Assert.That(names, Is.EqualTo(new[] { "ControlSeat", "CockpitName", "InternalTag" }));
        });
    }

    [Test]
    public async Task ProcessAsync_StripsReadonlyFromFields()
    {
        const string testCode =
            """
            class Program
            {
                readonly int A;
                private readonly int B;
                private static readonly string C;
                internal readonly float D;
                readonly int E = 42;
            }
            """;

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", testCode);
        var processor = new CodeSmallifier();
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb =
            {
                MinifierLevel = MinifierLevel.Lite,
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
        var actual = await result.GetTextAsync();
        // After readonly is stripped:
        // - A (readonly int), B (private readonly int → private int → int), E (readonly int)
        //   all become plain int and compact together.
        // - C (private static string → static string) and D (internal float) stay separate.
        var expected =
            """
            class Program
            {
                int A,B,E = 42;
                static string C;
                internal float D;
            }
            """;

        Assert.That(actual.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_ReadonlyStrippingEnablesCrossModifierCompaction()
    {
        // After readonly is stripped, fields that previously had different modifier sets can merge.
        const string testCode =
            """
            class Program
            {
                readonly int A;
                int B;
                private readonly int C;
                private int D;
            }
            """;

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", testCode);
        var processor = new CodeSmallifier();
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb =
            {
                MinifierLevel = MinifierLevel.Lite,
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
        var actual = await result.GetTextAsync();
        // After readonly is stripped:
        // - A (readonly int → int), B (int), C (private readonly int → private int → int),
        //   D (private int → int)
        // All four become plain int and compact into one declaration.
        var expected =
            """
            class Program
            {
                int A,B,C,D;
            }
            """;

        Assert.That(actual.ToString(), Is.EqualTo(expected));
    }
}
