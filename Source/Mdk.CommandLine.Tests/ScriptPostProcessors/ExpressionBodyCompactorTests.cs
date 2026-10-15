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
public class ExpressionBodyCompactorTests : DocumentProcessorTests<ExpressionBodyCompactor>
{
    /// <summary>
    ///     Runs the compactor over the given code.
    /// </summary>
    static async Task<string> CompactAsync(string code, MinifierLevel level = MinifierLevel.Lite)
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", code);
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb =
            {
                MinifierLevel = level,
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

        var result = await new ExpressionBodyCompactor().ProcessAsync(document, context);
        return (await result.GetTextAsync()).ToString();
    }

    [Test]
    public async Task ProcessAsync_WhenMinifierLevelIsBelowLite_DoesNotModifyDocument()
    {
        const string testCode =
            """
            class Program
            {
                int Value()
                {
                    return 1;
                }
            }
            """;

        var actual = await CompactAsync(testCode, MinifierLevel.StripComments);

        Assert.That(actual, Is.EqualTo(testCode));
    }

    [Test]
    public async Task ProcessAsync_WhenMethodBodyIsASingleReturn_UsesAnExpressionBody()
    {
        const string testCode =
            """
            class Program
            {
                int Value()
                {
                    return 1 + 2;
                }

                string Name(string prefix)
                {
                    return prefix + "x";
                }
            }
            """;

        var actual = await CompactAsync(testCode);

        const string expected =
            """
            class Program
            {
                int Value() => 1 + 2;

                string Name(string prefix) => prefix + "x";
            }
            """;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_WhenVoidMethodBodyIsASingleExpressionStatement_UsesAnExpressionBody()
    {
        const string testCode =
            """
            class Program
            {
                int _value;

                void Reset()
                {
                    _value = 0;
                }

                void Log()
                {
                    Echo("hi");
                }

                void Echo(string text)
                {
                    _value++;
                }
            }
            """;

        var actual = await CompactAsync(testCode);

        // A void method with a single expression statement is an expression body in C# 6 as well.
        const string expected =
            """
            class Program
            {
                int _value;

                void Reset() => _value = 0;

                void Log() => Echo("hi");

                void Echo(string text) => _value++;
            }
            """;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_WhenMethodBodyHasTwoStatements_LeavesItAlone()
    {
        const string testCode =
            """
            class Program
            {
                int Value()
                {
                    var result = 1;
                    return result;
                }

                void Empty()
                {
                }

                void Stop()
                {
                    return;
                }
            }
            """;

        var actual = await CompactAsync(testCode);

        Assert.That(actual, Is.EqualTo(testCode));
    }

    [Test]
    public async Task ProcessAsync_WhenMethodIsAnIterator_LeavesItAlone()
    {
        const string testCode =
            """
            class Program
            {
                System.Collections.Generic.IEnumerable<int> Numbers()
                {
                    yield return 1;
                }

                int Broken()
                {
                    throw new System.Exception("no");
                }
            }
            """;

        var actual = await CompactAsync(testCode);

        // A yield or throw statement is not an expression, and C# 6 has no throw expression.
        Assert.That(actual, Is.EqualTo(testCode));
    }

    [Test]
    public async Task ProcessAsync_WhenPropertyOnlyHasAGetterWithASingleReturn_UsesAnExpressionBody()
    {
        const string testCode =
            """
            class Program
            {
                int _value;

                int Value
                {
                    get { return _value; }
                }

                string Name
                {
                    get
                    {
                        return "pilot";
                    }
                }
            }
            """;

        var actual = await CompactAsync(testCode);

        const string expected =
            """
            class Program
            {
                int _value;

                int Value => _value;

                string Name => "pilot";
            }
            """;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_WhenIndexerOnlyHasAGetterWithASingleReturn_UsesAnExpressionBody()
    {
        const string testCode =
            """
            class Program
            {
                int[] _values;

                int this[int index]
                {
                    get { return _values[index]; }
                }
            }
            """;

        var actual = await CompactAsync(testCode);

        const string expected =
            """
            class Program
            {
                int[] _values;

                int this[int index] => _values[index];
            }
            """;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_WhenPropertyHasASetter_LeavesItAlone()
    {
        const string testCode =
            """
            class Program
            {
                int _value;

                int Value
                {
                    get { return _value; }
                    set { _value = value; }
                }

                int Stored { get; set; }

                int Restricted { get; private set; }
            }
            """;

        var actual = await CompactAsync(testCode);

        // C# 6 has no expression bodied accessors, so a property with a setter stays as it is.
        Assert.That(actual, Is.EqualTo(testCode));
    }

    [Test]
    public async Task ProcessAsync_WhenOperatorBodyIsASingleReturn_UsesAnExpressionBody()
    {
        const string testCode =
            """
            class Program
            {
                struct Speed
                {
                    public double Value;

                    public static Speed operator +(Speed a, Speed b)
                    {
                        return new Speed { Value = a.Value + b.Value };
                    }

                    public static implicit operator double(Speed speed)
                    {
                        return speed.Value;
                    }
                }
            }
            """;

        var actual = await CompactAsync(testCode);

        const string expected =
            """
            class Program
            {
                struct Speed
                {
                    public double Value;

                    public static Speed operator +(Speed a, Speed b) => new Speed { Value = a.Value + b.Value };

                    public static implicit operator double(Speed speed) => speed.Value;
                }
            }
            """;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_WhenConstructorBodyIsASingleStatement_LeavesItAlone()
    {
        const string testCode =
            """
            class Program
            {
                int _value;

                public Program()
                {
                    _value = 1;
                }
            }
            """;

        var actual = await CompactAsync(testCode);

        // C# 6 has no expression bodied constructors.
        Assert.That(actual, Is.EqualTo(testCode));
    }

    [Test]
    public async Task ProcessAsync_WhenBodyContainsACommentOrDirective_LeavesItAlone()
    {
        const string testCode =
            """
            class Program
            {
                int Value()
                {
                    // the answer
                    return 42;
                }

                int Other()
                {
            #pragma warning disable 162
                    return 43;
                }

                int Trailing()
                {
                    return 44; // for now
                }
            }
            """;

        var actual = await CompactAsync(testCode);

        // Anything but whitespace inside the body would be lost, so those bodies are left alone.
        // At Lite and above the comment stripper has already run, so in a pack this only protects
        // preprocessor directives and preserved regions.
        Assert.That(actual, Is.EqualTo(testCode));
    }
}
