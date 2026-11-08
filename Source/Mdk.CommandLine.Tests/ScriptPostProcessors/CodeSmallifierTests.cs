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
    static IEnumerable<MetadataReference> GetCoreReferences()
    {
        // Only System.Private.CoreLib is needed — it defines int, string, float, etc.
        yield return MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
    }

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
    public async Task ProcessAsync_DropsBracesAroundSingleStatementBlocks()
    {
        const string testCode =
            """
            class Program
            {
                public void M()
                {
                    if (A > 0)
                    {
                        F(A);
                    }
                    else
                    {
                        F(-A);
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        F(i);
                    }
                    while (A > 0)
                    {
                        F(A--);
                    }
                    switch (A)
                    {
                        case 1:
                        {
                            F(1);
                        }
                        case 2:
                        {
                            F(2);
                            F(3);
                        }
                    }
                    if (A > 4)
                    {
                        if (A > 5)
                        {
                            F(5);
                        }
                    }
                    if (A > 6)
                    {
                        if (A > 7)
                        {
                            F(7);
                        }
                    }
                    else
                    {
                        F(8);
                    }
                    void F(int x)
                    {
                        if (x > 0)
                        {
                            return;
                        }
                    }

                    int A;
                }
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
        // Braces are dropped where a single statement that is a valid embedded statement is the only
        // content: after if, else, for, while, a single-statement switch section, nested ifs, and inside
        // the local function's if. The statement keeps its own line and indent. The "if (A > 6)" block
        // keeps its braces: dropping them would rebind the else to the inner if (dangling else). The
        // two-statement switch section keeps its braces, and the method bodies (M and F) always keep
        // theirs: a method body without braces is not valid C#.
        var expected =
            """
            class Program
            {
                public void M()
                {
                    if (A > 0)
                        F(A);
                    else
                        F(-A);
                    for (int i = 0; i < 3; i++)
                        F(i);
                    while (A > 0)
                        F(A--);
                    switch (A)
                    {
                        case 1:
                            F(1);
                        case 2:
                        {
                            F(2);
                            F(3);
                        }
                    }
                    if (A > 4)
                        if (A > 5)
                            F(5);
                    if (A > 6)
                    {
                        if (A > 7)
                            F(7);
                    }
                    else
                        F(8);
                    void F(int x)
                    {
                        if (x > 0)
                            return;
                    }

                    int A;
                }
            }
            """;

        Assert.That(actual.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_DroppingBracesKeepsCommentsOnTheBraceLines()
    {
        const string testCode =
            """
            class Program
            {
                public void M()
                {
                    if (A > 0)
                    {
                        // after open brace
                        F(A); // at end of stmt
                        // before close brace
                    }
                    if (A < 0) // comment before brace, own line
                    {
                        F(-A);
                    }
                    if (A == 0) // comment before brace, one line
                    { F(0); }
                    if (A == 1) { F(1); } // comment after brace
                }
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
        // Every comment survives the unwrap. A comment is never left without a newline after it, so no
        // statement can end up inside one.
        // Every comment survives the unwrap. A comment is never left without a newline after it, so no
        // statement can end up inside one (which is why the one-line "if (A == 0)" case breaks after its
        // comment rather than landing F(0) inside it).
        var expected =
            """
            class Program
            {
                public void M()
                {
                    if (A > 0)
                        // after open brace
                        F(A); // at end of stmt
                        // before close brace
                    if (A < 0) // comment before brace, own line
                        F(-A);
                    if (A == 0) // comment before brace, one line
                    F(0);
                    if (A == 1) F(1); // comment after brace
                }
            }
            """;

        Assert.That(actual.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_StripsReadonlyFromFields()
    {
        // readonly is stripped for reference types (string), but preserved for value types (int, float).
        // This is because stripping readonly from a mutable struct field changes runtime behavior
        // (the compiler emits defensive copies for mutating operations on readonly struct fields).
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
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp)
            .WithMetadataReferences(GetCoreReferences());
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
        // After readonly is processed:
        // - A, B, E are int (struct), so readonly is preserved and they cannot compact together
        //   with plain int F (which doesn't have readonly).
        // - C is string (reference type), so readonly is stripped and it compacts with other
        //   static string fields.
        // - D is float (struct), so readonly is preserved.
        var expected =
            """
            class Program
            {
                readonly int A,B,E = 42;
                static string C;
                internal readonly float D;
            }
            """;

        Assert.That(actual.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_ReadonlyStrippingEnablesCrossModifierCompaction()
    {
        // After readonly is stripped for reference types, fields that previously had different
        // modifier sets can merge. Note: int is a struct, so readonly is preserved.
        const string testCode =
            """
            class Program
            {
                readonly string A;
                string B;
                private readonly string C;
                private string D;
            }
            """;

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp)
            .WithMetadataReferences(GetCoreReferences());
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
        // After readonly is stripped (string is a reference type):
        // - A (readonly string → string), B (string), C (private readonly string → private string → string),
        //   D (private string → string)
        // All four become plain string and compact into one declaration.
        var expected =
            """
            class Program
            {
                string A,B,C,D;
            }
            """;

        Assert.That(actual.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_PreservesReadonlyForStructFields()
    {
        // readonly should be preserved for user-defined struct fields because stripping it
        // changes runtime behavior — the compiler emits defensive copies for mutating operations
        // on readonly struct fields.
        const string testCode =
            """
            struct Cooldown
            {
                public double Remaining;
                public void Tick(double dt) { if (Remaining > 0) Remaining -= dt; }
            }

            class Program
            {
                readonly Cooldown _gate;
                Cooldown mutableGate;
            }
            """;

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp)
            .WithMetadataReferences(GetCoreReferences());
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
        // readonly should be preserved for the struct field _gate
        var expected =
            """
            struct Cooldown
            {
                public double Remaining;
                public void Tick(double dt) { if (Remaining > 0) Remaining -= dt; }
            }

            class Program
            {
                readonly Cooldown _gate;
                Cooldown mutableGate;
            }
            """;

        Assert.That(actual.ToString(), Is.EqualTo(expected));
    }
}