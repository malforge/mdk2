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
    static async Task<string> TrimAsync(
        string code,
        MinifierLevel level = MinifierLevel.Trim,
        bool protectProgram = false,
        bool withRegions = false)
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

        if (withRegions)
            document = await new RegionAnnotator().ProcessAsync(document, context);
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
    public async Task ProcessAsync_WhenPublicPropertyIsUsedFromASiblingType_RemovesTheAccessorModifierButKeepsPublic()
    {
        const string testCode =
            """
            class Program
            {
                public class Reader
                {
                    public double Value { get; private set; }
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

        // Value is read from Writer, a sibling of Reader, so it has to stay public. There is no
        // outside consumer left for the private setter to protect against, and a public property
        // with a public setter compiles and behaves the same, so the accessor modifier goes even
        // though the property itself stays public.
        const string expected =
            """
            class Program
            {
                class Reader
                {
                    public double Value { get; set; }
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
    public async Task ProcessAsync_WhenPublicIndexerIsUsedFromASiblingType_RemovesTheAccessorModifierButKeepsPublic()
    {
        const string testCode =
            """
            class Program
            {
                public class Reader
                {
                    double[] _values = new double[4];

                    public double this[int i]
                    {
                        get { return _values[i]; }
                        private set { _values[i] = value; }
                    }
                }

                class Writer
                {
                    void Copy(Reader reader)
                    {
                        var value = reader[0];
                    }
                }
            }
            """;

        var actual = await TrimAsync(testCode);

        // The indexer is read from Writer, a sibling of Reader, so it has to stay public, and the
        // private setter loses its modifier for the same reason a property's does.
        const string expected =
            """
            class Program
            {
                class Reader
                {
                    double[] _values = new double[4];

                    public double this[int i]
                    {
                        get { return _values[i]; }
                        set { _values[i] = value; }
                    }
                }

                class Writer
                {
                    void Copy(Reader reader)
                    {
                        var value = reader[0];
                    }
                }
            }
            """;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_WhenPublicPropertyAccessorsAlreadyHaveNoModifiers_LeavesThemUnchanged()
    {
        const string testCode =
            """
            class Program
            {
                public class Reader
                {
                    public double Value { get; set; }
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

        // Nothing to remove on Value's accessors, so the property is left exactly as written; only
        // Reader loses its public modifier.
        const string expected =
            """
            class Program
            {
                class Reader
                {
                    public double Value { get; set; }
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

    [Test]
    public async Task ProcessAsync_WhenTopLevelTypeIsExposedByAPublicProgramMember_KeepsPublic()
    {
        const string testCode =
            """
            class Program
            {
                public Config Current;

                void Run()
                {
                }
            }

            class Tools
            {
                static int SpeedOf(Program program)
                {
                    return program.Current.Speed;
                }
            }

            public class Config
            {
                public int Speed;
            }
            """;

        var actual = await TrimAsync(testCode);

        // The packed script becomes the body of the game's public Program class, so a public member of
        // Program (here Current, read by Tools) exposes its type as public even though Program has no
        // modifier in the source. Without treating Program as public, Config would be demoted and the
        // game compiler would fail with CS0052 once the script is wrapped in a public class.
        const string expected =
            """
            class Program
            {
                public Config Current;

                void Run()
                {
                }
            }

            class Tools
            {
                static int SpeedOf(Program program)
                {
                    return program.Current.Speed;
                }
            }

            public class Config
            {
                public int Speed;
            }
            """;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_WhenOverrideAccessorMatchesAPreservedBase_KeepsTheAccessorModifier()
    {
        const string testCode =
            """
            class Program
            {
                #region mdk preserve
                public class Gauge
                {
                    public virtual int Level { get; protected set; }
                }
                #endregion

                class Meter : Gauge
                {
                    public override int Level { get; protected set; }
                }
            }
            """;

        var actual = await TrimAsync(testCode, withRegions: true);

        // Gauge is preserved, so its protected set is kept. The override in Meter must match the base
        // accessor exactly (CS0507), so its protected set has to stay as well. The non-preserved control
        // pair below is stripped in step.
        Assert.That(actual, Does.Contain("public override int Level { get; protected set; }"));
    }

    [Test]
    public async Task ProcessAsync_WhenOverrideAccessorMatchesAnUnpreservedBase_StripsTheAccessorModifier()
    {
        const string testCode =
            """
            class Program
            {
                public class Gauge
                {
                    public virtual int Level { get; protected set; }
                }

                class Meter : Gauge
                {
                    public override int Level { get; protected set; }
                }
            }
            """;

        var actual = await TrimAsync(testCode, withRegions: true);

        // Nothing is preserved, so both the base and the override are trimmed in step: the accessors
        // become public to match, and the public on the accessors goes with the public on the property.
        Assert.That(actual, Does.Not.Contain("protected set"));
    }

    [Test]
    public async Task ProcessAsync_WhenMemberImplementsAnInterfaceForADerivedType_KeepsPublic()
    {
        const string testCode =
            """
            class Program
            {
                void Run()
                {
                }
            }

            interface ITick
            {
                void Tick();
            }

            class BaseModule
            {
                public void Tick()
                {
                }
            }

            class Beacon : BaseModule, ITick
            {
            }
            """;

        var actual = await TrimAsync(testCode);

        // Tick is the implementation of ITick.Tick for Beacon through BaseModule. ISymbol.IsInterfaceImplementation
        // only inspects BaseModule's own interfaces, so without the derived-type check Tick would be made
        // private and Beacon would no longer implement ITick (CS0535).
        const string expected = testCode;
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task ProcessAsync_WhenOverrideChainHasAPreservedTop_KeepsTheAccessorModifier()
    {
        const string testCode =
            """
            class Program
            {
                #region mdk preserve
                public class C
                {
                    public virtual int Level { get; protected set; }
                }
                #endregion

                class B : C
                {
                    public override int Level { get; protected set; }
                }

                class A : B
                {
                    public override int Level { get; protected set; }
                }
            }
            """;

        var actual = await TrimAsync(testCode, withRegions: true);

        // C is preserved, so it keeps its protected set. B must match C and A must match B, so the whole
        // chain (A : B : C) keeps the restricted accessor even though only the top is preserved. A naive
        // check of only the direct base would strip A and produce CS0507 against B.
        Assert.That(actual.Split("protected set").Length - 1, Is.EqualTo(3), $"actual was:\n{actual}");
    }

    [Test]
    public async Task ProcessAsync_WhenOverrideChainHasAPreservedMiddle_KeepsTheAccessorModifier()
    {
        const string testCode =
            """
            class Program
            {
                public class C
                {
                    public virtual int Level { get; protected set; }
                }

                #region mdk preserve
                class B : C
                {
                    public override int Level { get; protected set; }
                }
                #endregion

                class A : B
                {
                    public override int Level { get; protected set; }
                }
            }
            """;

        var actual = await TrimAsync(testCode, withRegions: true);

        // B is preserved, so it keeps its protected set. C is its base and A its derived type, so both must
        // keep the restricted accessor to stay in step. This requires walking down as well as up the chain.
        Assert.That(actual.Split("protected set").Length - 1, Is.EqualTo(3), $"actual was:\n{actual}");
    }

    [Test]
    public async Task ProcessAsync_WhenOverrideChainBranchesAroundAPreservedBase_KeepsTheAccessorModifier()
    {
        const string testCode =
            """
            class Program
            {
                public class Root
                {
                    public virtual int Level { get; protected set; }
                }

                #region mdk preserve
                class Middle : Root
                {
                    public override int Level { get; protected set; }
                }
                #endregion

                class Left : Middle
                {
                    public override int Level { get; protected set; }
                }

                class Right : Middle
                {
                    public override int Level { get; protected set; }
                }
            }
            """;

        var actual = await TrimAsync(testCode, withRegions: true);

        // Middle is preserved, so it keeps its protected set. Left and Right override it (kept via the walk up
        // the chain), and Root is overridden by it (kept via the walk down the chain, since Root only sees Middle
        // as its override through one of the branches). The whole branching chain must stay in step.
        Assert.That(actual.Split("protected set").Length - 1, Is.EqualTo(4), $"actual was:\n{actual}");
    }

    [Test]
    public async Task ProcessAsync_WhenOverrideChainIsUnpreserved_StripsAllAccessorModifiers()
    {
        const string testCode =
            """
            class Program
            {
                public class C
                {
                    public virtual int Level { get; protected set; }
                }

                class B : C
                {
                    public override int Level { get; protected set; }
                }

                class A : B
                {
                    public override int Level { get; protected set; }
                }
            }
            """;

        var actual = await TrimAsync(testCode, withRegions: true);

        // Nothing in the chain is preserved or out of scope, so every link is reduced in step to a public
        // accessor and no restricted accessor remains.
        Assert.That(actual, Does.Not.Contain("protected set"));
    }

    [Test]
    public async Task ProcessAsync_WhenMemberImplementsAnInterfaceForANestedDerivedType_KeepsPublic()
    {
        const string testCode =
            """
            class Program
            {
                void Run()
                {
                }
            }

            interface ITick
            {
                void Tick();
            }

            class Base
            {
                public void Tick()
                {
                }
            }

            class Middle : Base
            {
            }

            class Leaf : Middle, ITick
            {
            }
            """;

        var actual = await TrimAsync(testCode);

        // Tick is the implementation of ITick.Tick for Leaf, reached through two levels (Leaf : Middle : Base).
        // The derived-type check must walk the whole base chain, or Tick would be made private and Leaf would
        // no longer implement ITick (CS0535).
        Assert.That(actual, Does.Contain("public void Tick()"));
    }

    [Test]
    public async Task ProcessAsync_WhenTopLevelTypeIsExposedThroughANestedProgramMember_KeepsPublic()
    {
        const string testCode =
            """
            class Program
            {
                public class Option
                {
                    public Model Value;
                }

                void Run()
                {
                }
            }

            class Reader
            {
                static int Read(Program.Option option)
                {
                    return option.Value.score;
                }
            }

            public class Model
            {
                public int score;
            }
            """;

        var actual = await TrimAsync(testCode);

        // Model is exposed by the public member Option.Value, which is itself reached through Program's public
        // Option. The exposure walks up to the entry class, which is public in the game, so Model must stay
        // public rather than being demoted against an internal Program.
        Assert.That(actual, Does.Contain("public class Model"));
    }

    [Test]
    public async Task ProcessAsync_WhenTopLevelEnumIsAPublicProgramMethodParameter_KeepsPublic()
    {
        const string testCode =
            """
            class Program
            {
                public void Report(Severity level)
                {
                }

                void Run()
                {
                }
            }

            public enum Severity
            {
                Info,
                Warning,
                Error
            }

            class Watchdog
            {
                readonly Program _program;

                public Watchdog(Program program)
                {
                    _program = program;
                }

                public void Trip()
                {
                    _program.Report(Severity.Error);
                }
            }
            """;

        var actual = await TrimAsync(testCode);

        // Severity is a parameter type of a public method of Program, so it must stay public even though
        // Program has no accessibility modifier in the source.
        Assert.That(actual, Does.Contain("public enum Severity"));
    }

    [Test]
    public async Task ProcessAsync_WhenTopLevelDelegateIsAPublicProgramField_KeepsPublic()
    {
        const string testCode =
            """
            class Program
            {
                public Formatter Format = text => text;

                void Run()
                {
                }
            }

            public delegate string Formatter(string text);

            class Skin
            {
                readonly Program _program;

                public Skin(Program program)
                {
                    _program = program;
                }

                public void Apply()
                {
                    _program.Format = _program.Format;
                }
            }
            """;

        var actual = await TrimAsync(testCode);

        // Formatter is the type of a public field of Program, so it must stay public.
        Assert.That(actual, Does.Contain("public delegate string Formatter"));
    }
}
