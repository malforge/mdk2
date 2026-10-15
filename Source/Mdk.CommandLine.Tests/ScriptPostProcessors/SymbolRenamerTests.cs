using System.Collections.Immutable;
using System.Reflection;
using FakeItEasy;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;
using Mdk.CommandLine.Shared;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

    [Test]
    public async Task ProcessAsync_WithPreservedEnumInsideGenericOuterType_KeepsEnumAndMembers()
    {
        const string testCode =
            """
            namespace TestNamespace
            {
                #region mdk preserve
                class Outer<T>
                {
                    enum PreservedEnum
                    {
                        Alpha,
                        Beta
                    }

                    public PreservedEnum Value = PreservedEnum.Alpha;
                }
                #endregion

                class Program
                {
                    void Test()
                    {
                        var outer = new Outer<int>();
                        _ = outer.Value;
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
        var preservedEnum = resultRoot!.DescendantNodes().OfType<EnumDeclarationSyntax>().Single();
        var enumMemberNames = preservedEnum.Members.Select(m => m.Identifier.ValueText).ToArray();
        var enumReferences = resultRoot.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Where(m => m.Expression is IdentifierNameSyntax { Identifier.ValueText: "PreservedEnum" })
            .Select(m => m.Name.Identifier.ValueText)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(preservedEnum.Identifier.ValueText, Is.EqualTo("PreservedEnum"));
            Assert.That(enumMemberNames, Is.EqualTo(new[] { "Alpha", "Beta" }));
            Assert.That(enumReferences, Is.EqualTo(new[] { "Alpha" }));
        });
    }

    [Test]
    public async Task ProcessAsync_WithRegistryOnlyPreservedEnumInsideGenericOuterType_KeepsEnumAndMembers()
    {
        const string testCode =
            """
            namespace TestNamespace
            {
                #region mdk preserve
                class Outer<T>
                {
                    enum PreservedEnum
                    {
                        Alpha,
                        Beta
                    }

                    public PreservedEnum Value = PreservedEnum.Alpha;
                }
                #endregion

                class Program
                {
                    void Test()
                    {
                        var outer = new Outer<int>();
                        _ = outer.Value;
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
        var flattenedText = await flattenedDocument.GetTextAsync();
        var annotationFreeDocument = flattenedDocument.Project.Solution
            .WithDocumentText(flattenedDocument.Id, flattenedText)
            .GetDocument(flattenedDocument.Id)!;
        var semanticModel = await annotationFreeDocument.GetSemanticModelAsync();
        var enumSymbol = semanticModel!.GetDeclaredSymbol((await annotationFreeDocument.GetSyntaxRootAsync())!.DescendantNodes().OfType<EnumDeclarationSyntax>().Single());
        var registryType = typeof(SymbolRenamer).Assembly.GetType("Mdk.CommandLine.IngameScript.Pack.DefaultProcessors.PreservedDeclarationRegistry");
        var containsMethod = registryType!.GetMethod("Contains", BindingFlags.Public | BindingFlags.Static);
        var registryContains = (bool?)containsMethod!.Invoke(null, [enumSymbol!, context]);
        var result = await processor.ProcessAsync(annotationFreeDocument, context);
        var resultRoot = await result.GetSyntaxRootAsync();
        var preservedEnum = resultRoot!.DescendantNodes().OfType<EnumDeclarationSyntax>().Single();
        var enumMemberNames = preservedEnum.Members.Select(m => m.Identifier.ValueText).ToArray();
        var enumReferences = resultRoot.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Where(m => m.Expression is IdentifierNameSyntax { Identifier.ValueText: "PreservedEnum" })
            .Select(m => m.Name.Identifier.ValueText)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(registryContains, Is.True);
            Assert.That(preservedEnum.Identifier.ValueText, Is.EqualTo("PreservedEnum"));
            Assert.That(enumMemberNames, Is.EqualTo(new[] { "Alpha", "Beta" }));
            Assert.That(enumReferences, Is.EqualTo(new[] { "Alpha" }));
        });
    }

    [Test]
    public async Task ProcessAsync_WhenPreservedTypeOverridesUnpreservedBase_OverrideStillMatchesTheBase()
    {
        // Issue #182: the preserved override keeps its name, so the base member it overrides has to keep
        // that name too, or the override resolves against nothing (CS0115).
        const string testCode =
            """
            class Program
            {
                void Test()
                {
                    var middle = new Middle();
                    var value = middle.X;
                }

                class Root
                {
                    public virtual int X { get; }
                }

                #region mdk preserve
                class Middle : Root
                {
                    public override int X { get; }
                }
                #endregion
            }
            """;

        var resultRoot = await RenameAsync(testCode);
        var classes = resultRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().ToArray();
        var middle = classes.Single(c => c.Identifier.ValueText == "Middle");
        var baseTypeName = middle.BaseList!.Types.Single().Type.ToString();
        var root = classes.Single(c => c.Identifier.ValueText == baseTypeName);

        var overrideName = middle.Members.OfType<PropertyDeclarationSyntax>().Single().Identifier.ValueText;
        var baseName = root.Members.OfType<PropertyDeclarationSyntax>().Single().Identifier.ValueText;

        Assert.Multiple(() =>
        {
            Assert.That(overrideName, Is.EqualTo("X"), "The preserved override keeps its name.");
            Assert.That(baseName, Is.EqualTo(overrideName), "The base member must keep the name the preserved override still uses.");
        });
    }

    [Test]
    public async Task ProcessAsync_WhenPreservedTypeIsAtTheEndOfAnOverrideChain_WholeChainKeepsTheName()
    {
        // Only the leaf is preserved, so every member it overrides up the chain has to keep the name too.
        const string testCode =
            """
            class Program
            {
                void Test()
                {
                    var leaf = new Leaf();
                    var value = leaf.X;
                }

                class Root
                {
                    public virtual int X { get; }
                }

                class Middle : Root
                {
                    public override int X { get; }
                }

                #region mdk preserve
                class Leaf : Middle
                {
                    public override int X { get; }
                }
                #endregion
            }
            """;

        var resultRoot = await RenameAsync(testCode);
        var names = resultRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .Select(p => p.Identifier.ValueText).ToArray();

        Assert.That(names, Is.EqualTo(new[] { "X", "X", "X" }));
    }

    [Test]
    public async Task ProcessAsync_WhenPreservedTypeImplementsUnpreservedInterface_MemberNamesStillMatch()
    {
        // The preserved implementation cannot be renamed, so the interface member it implements cannot be either.
        const string testCode =
            """
            class Program
            {
                void Test()
                {
                    IShape shape = new Box();
                    var value = shape.Area;
                }

                interface IShape
                {
                    int Area { get; }
                }

                #region mdk preserve
                class Box : IShape
                {
                    public int Area { get { return 1; } }
                }
                #endregion
            }
            """;

        var resultRoot = await RenameAsync(testCode);
        var names = resultRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .Select(p => p.Identifier.ValueText).ToArray();

        Assert.That(names, Is.EqualTo(new[] { "Area", "Area" }));
    }

    [Test]
    public async Task ProcessAsync_WhenAPreservedOverrideHasAnUnpreservedSibling_BothKeepTheName()
    {
        // Pinning the base member is not enough: every other override of it shares the same name, so renaming
        // one of those leaves it overriding a member which is no longer called that.
        const string testCode =
            """
            class Program
            {
                void Test()
                {
                    Root root = new Other();
                    var value = root.X + new Box().X;
                }

                class Root
                {
                    public virtual int X { get { return 0; } }
                }

                class Other : Root
                {
                    public override int X { get { return 1; } }
                }

                #region mdk preserve
                class Box : Root
                {
                    public override int X { get { return 2; } }
                }
                #endregion
            }
            """;

        var resultRoot = await RenameAsync(testCode);
        var names = resultRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .Select(p => p.Identifier.ValueText).ToArray();

        Assert.That(names, Is.EqualTo(new[] { "X", "X", "X" }));
    }

    [Test]
    public async Task ProcessAsync_WhenAPreservedImplementationHasAnUnpreservedSibling_BothKeepTheName()
    {
        // The same for interfaces: the other implementor would stop implementing the member it still declares.
        const string testCode =
            """
            class Program
            {
                void Test()
                {
                    IShape shape = new Circle();
                    var value = shape.Area + new Box().Area;
                }

                interface IShape
                {
                    int Area { get; }
                }

                class Circle : IShape
                {
                    public int Area { get { return 1; } }
                }

                #region mdk preserve
                class Box : IShape
                {
                    public int Area { get { return 2; } }
                }
                #endregion
            }
            """;

        var resultRoot = await RenameAsync(testCode);
        var names = resultRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .Select(p => p.Identifier.ValueText).ToArray();

        Assert.That(names, Is.EqualTo(new[] { "Area", "Area", "Area" }));
    }

    [Test]
    public async Task ProcessAsync_WhenAPreservedMemberBothOverridesAndImplements_AllThreeKeepTheName()
    {
        // A member can be an override and an interface implementation at once; both ends have to be pinned.
        const string testCode =
            """
            class Program
            {
                void Test()
                {
                    IShape shape = new Box();
                    var value = shape.X;
                }

                interface IShape
                {
                    int X { get; }
                }

                class Root
                {
                    public virtual int X { get { return 0; } }
                }

                #region mdk preserve
                class Box : Root, IShape
                {
                    public override int X { get { return 1; } }
                }
                #endregion
            }
            """;

        var resultRoot = await RenameAsync(testCode);
        var names = resultRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .Select(p => p.Identifier.ValueText).ToArray();

        Assert.That(names, Is.EqualTo(new[] { "X", "X", "X" }));
    }

    [Test]
    public async Task ProcessAsync_WhenAPreservedMemberImplementsTwoInterfaces_BothInterfacesKeepTheName()
    {
        // One member can implement same-named members of several interfaces, and every one of them is pinned.
        const string testCode =
            """
            class Program
            {
                void Test()
                {
                    IA a = new Box();
                    IB b = new Box();
                    var value = a.X + b.X;
                }

                interface IA
                {
                    int X { get; }
                }

                interface IB
                {
                    int X { get; }
                }

                #region mdk preserve
                class Box : IA, IB
                {
                    public int X { get { return 1; } }
                }
                #endregion
            }
            """;

        var resultRoot = await RenameAsync(testCode);
        var names = resultRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .Select(p => p.Identifier.ValueText).ToArray();

        Assert.That(names, Is.EqualTo(new[] { "X", "X", "X" }));
    }

    [Test]
    public async Task ProcessAsync_WhenAPreservedEventImplementsAnInterfaceEvent_BothKeepTheName()
    {
        // Event fields declare their symbols on the variable declarators, so they are easily missed when walking
        // member declarations.
        const string testCode =
            """
            class Program
            {
                void Test()
                {
                    IBus bus = new Bus();
                    bus.Ping += () => { };
                }

                interface IBus
                {
                    event System.Action Ping;
                }

                #region mdk preserve
                class Bus : IBus
                {
                    public event System.Action Ping;
                }
                #endregion
            }
            """;

        var resultRoot = await RenameAsync(testCode);
        var names = resultRoot.DescendantNodes().OfType<EventFieldDeclarationSyntax>()
            .SelectMany(e => e.Declaration.Variables)
            .Select(v => v.Identifier.ValueText).ToArray();

        Assert.That(names, Is.EqualTo(new[] { "Ping", "Ping" }));
    }

    [Test]
    public async Task ProcessAsync_WhenAPreservedBaseMemberImplementsAnInterfaceForADerivedType_BothKeepTheName()
    {
        // The implementing member can sit in a base class which knows nothing about the interface, so the pair can
        // only be resolved from the type which declares the interface.
        const string testCode =
            """
            class Program
            {
                void Test()
                {
                    IShape shape = new Derived();
                    var value = shape.Area;
                }

                interface IShape
                {
                    int Area { get; }
                }

                class Base
                {
                    #region mdk preserve
                    public int Area { get { return 1; } }
                    #endregion
                }

                class Derived : Base, IShape
                {
                }
            }
            """;

        var resultRoot = await RenameAsync(testCode);
        var names = resultRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .Select(p => p.Identifier.ValueText).ToArray();

        Assert.That(names, Is.EqualTo(new[] { "Area", "Area" }));
    }

    [Test]
    public async Task ProcessAsync_WhenAPreservedInterfaceIsImplementedByABaseMember_BothKeepTheName()
    {
        // The same relation from the other end: this time the interface member is the preserved one.
        const string testCode =
            """
            class Program
            {
                void Test()
                {
                    IShape shape = new Derived();
                    var value = shape.Area;
                }

                #region mdk preserve
                interface IShape
                {
                    int Area { get; }
                }
                #endregion

                class Base
                {
                    public int Area { get { return 1; } }
                }

                class Derived : Base, IShape
                {
                }
            }
            """;

        var resultRoot = await RenameAsync(testCode);
        var names = resultRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .Select(p => p.Identifier.ValueText).ToArray();

        Assert.That(names, Is.EqualTo(new[] { "Area", "Area" }));
    }

    [Test]
    public async Task ProcessAsync_WhenAPreservedNameLooksLikeAGeneratedName_TheGeneratorDoesNotReuseIt()
    {
        // The generator starts at "A", and a name kept by a preserved declaration is still taken.
        const string testCode =
            """
            class Program
            {
                void Test()
                {
                    var box = new Box();
                    var value = box.Value;
                }

                #region mdk preserve
                class A
                {
                }
                #endregion

                class Box
                {
                    public int Value { get { return 1; } }
                }
            }
            """;

        var resultRoot = await RenameAsync(testCode);
        var classNames = resultRoot.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Select(c => c.Identifier.ValueText).ToArray();

        Assert.That(classNames, Is.Unique);
    }

    static async Task<SyntaxNode> RenameAsync(string testCode)
    {
        var workspace = new AdhocWorkspace();
        // Without references the semantic model cannot bind anything from the framework, which quietly changes
        // what the renamer sees.
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp).AddMetadataReferences(PlatformReferences());
        var document = project.AddDocument("TestDocument", testCode);
        var context = CreateContext();
        var annotated = await new RegionAnnotator().ProcessAsync(document, context);
        annotated = await new SymbolProtectionAnnotator().ProcessAsync(annotated, context);
        var result = await new SymbolRenamer().ProcessAsync(annotated, context);
        var resultRoot = (await result.GetSyntaxRootAsync())!;
        AssertCompiles(resultRoot);
        return resultRoot;
    }

    /// <summary>
    ///     Matching names are only a proxy for what actually matters: the renamed script still has to compile.
    /// </summary>
    /// <param name="root"></param>
    static void AssertCompiles(SyntaxNode root)
    {
        var compilation = CSharpCompilation.Create("RenamedScript",
            [CSharpSyntaxTree.Create((CSharpSyntaxNode)root)],
            PlatformReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.That(errors, Is.Empty, () => string.Join(Environment.NewLine, errors.Select(e => e.ToString())) + Environment.NewLine + root.ToFullString());
    }

    static IEnumerable<MetadataReference> PlatformReferences() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path));

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
