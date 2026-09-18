using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
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
public class AccessibilityTrimmerTests : DocumentProcessorTests<AccessibilityTrimmer>
{
    /// <summary>
    ///     Runs the trimmer over the given code, optionally letting the <see cref="SymbolProtectionAnnotator" /> mark the
    ///     Program entry points first, exactly as the pack pipeline does.
    /// </summary>
    static async Task<string> TrimAsync(string code, MinifierLevel level = MinifierLevel.Trim, bool protectProgram = false)
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp)
            .WithMetadataReferences(GetCoreReferences());
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

        if (protectProgram)
            document = await new SymbolProtectionAnnotator().ProcessAsync(document, context);
        var result = await new AccessibilityTrimmer().ProcessAsync(document, context);
        return (await result.GetTextAsync()).ToString();
    }

    static IEnumerable<MetadataReference> GetCoreReferences()
    {
        var coreAssemblies = new[]
        {
            typeof(object).Assembly,
            typeof(Console).Assembly,
            typeof(Enumerable).Assembly,
            typeof(List<>).Assembly,
            typeof(Task).Assembly
        };

        var references = coreAssemblies.Select(assembly => MetadataReference.CreateFromFile(assembly.Location)).ToList();
        references.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location));
        return references;
    }

    [Test]
    public async Task ProcessAsync_WhenMinifierLevelIsBelowTrim_DoesNotModifyDocument()
    {
        const string testCode =
            """
            class Program
            {
                public int Counter;

                void Run()
                {
                    Counter++;
                }
            }
            """;

        var actual = await TrimAsync(testCode, MinifierLevel.None);

        Assert.That(actual, Is.EqualTo(testCode));
    }

    [Test]
    public async Task ProcessAsync_WhenNestedTypeIsOnlyUsedInsideItsContainer_StripsPublic()
    {
        const string testCode =
            """
            class Program
            {
                public class Helper
                {
                    public int Value;
                }

                void Run()
                {
                    var helper = new Helper();
                    Echo(helper.Value.ToString());
                }

                void Echo(string text)
                {
                }
            }
            """;

        var actual = await TrimAsync(testCode);

        // Helper becomes private to Program, which every other member of Program can still reach.
        // Value is read from Run, which is outside Helper, so it has to stay public.
        const string expected =
            """
            class Program
            {
                class Helper
                {
                    public int Value;
                }

                void Run()
                {
                    var helper = new Helper();
                    Echo(helper.Value.ToString());
                }

                void Echo(string text)
                {
                }
            }
            """;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_WhenTopLevelTypeIsPublic_StripsPublic()
    {
        const string testCode =
            """
            public static class Version
            {
                public const string Number = "1";
            }

            class Program
            {
                void Run()
                {
                    Echo(Version.Number);
                }

                void Echo(string text)
                {
                }
            }
            """;

        var actual = await TrimAsync(testCode);

        // A top level type becomes internal, which is still reachable from everything in the script.
        // Number is read from Program, so it has to stay public.
        const string expected =
            """
            static class Version
            {
                public const string Number = "1";
            }

            class Program
            {
                void Run()
                {
                    Echo(Version.Number);
                }

                void Echo(string text)
                {
                }
            }
            """;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_WhenMembersAreOnlyUsedInsideTheirOwnType_StripsPublic()
    {
        const string testCode =
            """
            class Program
            {
                public int Counter;

                public void Tick()
                {
                    Counter++;
                }

                void Run()
                {
                    Tick();
                }
            }
            """;

        var actual = await TrimAsync(testCode);

        const string expected =
            """
            class Program
            {
                int Counter;

                void Tick()
                {
                    Counter++;
                }

                void Run()
                {
                    Tick();
                }
            }
            """;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_WhenPropertyHasARestrictedAccessor_RemovesTheAccessorModifierToo()
    {
        const string testCode =
            """
            class Program
            {
                public class Ship
                {
                    public double Speed { get; private set; }

                    public void Update()
                    {
                        Speed = Speed + 1;
                    }
                }

                void Run()
                {
                    var ship = new Ship();
                    ship.Update();
                }
            }
            """;

        var actual = await TrimAsync(testCode);

        // An accessor has to be more restrictive than its property (CS0273), so the private on the
        // setter has to go along with the public on the property. Both end up private to Ship,
        // which is what they were worth before.
        const string expected =
            """
            class Program
            {
                class Ship
                {
                    double Speed { get; set; }

                    public void Update()
                    {
                        Speed = Speed + 1;
                    }
                }

                void Run()
                {
                    var ship = new Ship();
                    ship.Update();
                }
            }
            """;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_WhenMemberIsUsedFromASiblingType_KeepsPublic()
    {
        const string testCode =
            """
            class Program
            {
                public class Reader
                {
                    public int Value;
                    public int Unread;
                }

                class Writer
                {
                    void Copy(Reader reader)
                    {
                        var value = reader.Value;
                    }
                }
            }
            """;

        var actual = await TrimAsync(testCode);

        // Value is read from Writer, a sibling of Reader, so a private Value would not compile.
        // Unread has no references at all, so it can be private.
        const string expected =
            """
            class Program
            {
                class Reader
                {
                    public int Value;
                    int Unread;
                }

                class Writer
                {
                    void Copy(Reader reader)
                    {
                        var value = reader.Value;
                    }
                }
            }
            """;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_WhenMemberImplementsAnInterfaceMember_KeepsPublic()
    {
        const string testCode =
            """
            class Program
            {
                public interface IThing
                {
                    int Value { get; }
                    void Do();
                }

                public class Thing : IThing
                {
                    public int Value { get { return 1; } }

                    public void Do()
                    {
                    }
                }

                void Run()
                {
                    IThing thing = new Thing();
                    thing.Do();
                }
            }
            """;

        var actual = await TrimAsync(testCode);

        // An implicit interface implementation must be public, even though nothing calls
        // Thing.Value or Thing.Do through the class itself.
        const string expected =
            """
            class Program
            {
                interface IThing
                {
                    int Value { get; }
                    void Do();
                }

                class Thing : IThing
                {
                    public int Value { get { return 1; } }

                    public void Do()
                    {
                    }
                }

                void Run()
                {
                    IThing thing = new Thing();
                    thing.Do();
                }
            }
            """;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_WhenMemberIsVirtualOrOverride_KeepsPublicInStepWithTheBase()
    {
        const string testCode =
            """
            class Program
            {
                public abstract class Shape
                {
                    public abstract double Area();

                    public virtual string Name()
                    {
                        return "shape";
                    }
                }

                public class Circle : Shape
                {
                    public override double Area()
                    {
                        return 3;
                    }

                    public override string Name()
                    {
                        return "circle";
                    }
                }

                void Run()
                {
                    Shape shape = new Circle();
                    Echo(shape.Area().ToString() + shape.Name());
                }

                void Echo(string text)
                {
                }
            }
            """;

        var actual = await TrimAsync(testCode);

        // An override must keep the accessibility of what it overrides, and a virtual or abstract
        // member cannot be private at all.
        const string expected =
            """
            class Program
            {
                abstract class Shape
                {
                    public abstract double Area();

                    public virtual string Name()
                    {
                        return "shape";
                    }
                }

                class Circle : Shape
                {
                    public override double Area()
                    {
                        return 3;
                    }

                    public override string Name()
                    {
                        return "circle";
                    }
                }

                void Run()
                {
                    Shape shape = new Circle();
                    Echo(shape.Area().ToString() + shape.Name());
                }

                void Echo(string text)
                {
                }
            }
            """;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_WhenMembersAreTheProgramEntryPoints_KeepsPublic()
    {
        const string testCode =
            """
            public class Program
            {
                public int Runtime;

                public Program()
                {
                    Runtime = 1;
                }

                public void Main(string argument)
                {
                    Runtime++;
                }

                public void Save()
                {
                }
            }
            """;

        var actual = await TrimAsync(testCode, protectProgram: true);

        // The game calls the constructor, Main and Save: they are protected symbols and must stay
        // public. Runtime is only used inside Program, so it goes.
        const string expected =
            """
            public class Program
            {
                int Runtime;

                public Program()
                {
                    Runtime = 1;
                }

                public void Main(string argument)
                {
                    Runtime++;
                }

                public void Save()
                {
                }
            }
            """;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_WhenTypeIsExposedByAPublicSignature_KeepsPublic()
    {
        const string testCode =
            """
            class Program
            {
                public class Config
                {
                    public int Speed;
                }

                public class Scratch
                {
                    public int Temp;

                    public void Reset()
                    {
                        Temp = 0;
                    }
                }

                public Config Current;

                void Run()
                {
                    var scratch = new Scratch();
                    scratch.Reset();
                }
            }

            class Tools
            {
                public static int SpeedOf(Program program)
                {
                    return program.Current.Speed;
                }
            }
            """;

        var actual = await TrimAsync(testCode);

        // Current stays public because Tools reads it, and that keeps Config public too: a private
        // Config would be less accessible than the public field that exposes it (CS0052).
        // Scratch is only used inside a method body, so nothing is exposed and it can be private.
        const string expected =
            """
            class Program
            {
                public class Config
                {
                    public int Speed;
                }

                class Scratch
                {
                    int Temp;

                    public void Reset()
                    {
                        Temp = 0;
                    }
                }

                public Config Current;

                void Run()
                {
                    var scratch = new Scratch();
                    scratch.Reset();
                }
            }

            class Tools
            {
                static int SpeedOf(Program program)
                {
                    return program.Current.Speed;
                }
            }
            """;
        Assert.That(actual, Is.EqualTo(expected));
    }
}
