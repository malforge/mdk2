using System.Collections.Immutable;
using System.Reflection;
using System.Text;
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

namespace MDK.CommandLine.Tests.ScriptCombiners;

[TestFixture]
public class CombinerTests
{
    // Helper method to get the required references using Assembly.Load and direct System.Runtime reference
    static IEnumerable<MetadataReference> GetCoreReferences()
    {
        var coreAssemblies = new[]
        {
            typeof(object).Assembly, // System.Private.CoreLib
            typeof(Console).Assembly, // System.Console
            typeof(Enumerable).Assembly, // System.Linq
            typeof(List<>).Assembly, // System.Collections
            typeof(Task).Assembly, // System.Threading.Tasks
            typeof(StringBuilder).Assembly // System.Text
        };

        // Create metadata references for core assemblies
        var references = coreAssemblies.Select(assembly => MetadataReference.CreateFromFile(assembly.Location)).ToList();

        // Explicitly add the System.Runtime.dll reference
        var systemRuntimePath = Assembly.Load("System.Runtime").Location;
        references.Add(MetadataReference.CreateFromFile(systemRuntimePath));

        return references;
    }

    [Test]
    public async Task CombineAsync_With5DocumentsWithVariousUsingDeclarations_CombinesAndUnifiesUsingDeclarations()
    {
        // Arrange
        var workspace = new AdhocWorkspace();

        var project = workspace.AddProject("TestProject", LanguageNames.CSharp)
            .WithMetadataReferences(GetCoreReferences())
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var document1 = project.AddDocument("TestDocument1",
            """
            using System;
            class Program {
                Int32 i;
            }

            """);
        project = document1.Project;
        var document2 = project.AddDocument("TestDocument2",
            """
            using System;
            using System.Collections.Generic;
            class OtherClass {
                List<Int32> list;
            }

            """);
        project = document2.Project;
        var document3 = project.AddDocument("TestDocument3",
            """
            using System.Linq;
            class AnotherClass {
                IEnumerable<Int32> enumerable;
                void Method() {
                    var query = enumerable.Select(i => i);
                }
            }

            """);
        project = document3.Project;
        var document4 = project.AddDocument("TestDocument4",
            """
            using System.Text;
            using System.Collections.Generic;
            class YetAnotherClass {
                StringBuilder builder;
                List<String> list;
            }

            """);
        project = document4.Project;
        var document5 = project.AddDocument("TestDocument5",
            """
            using System.Threading.Tasks;
            class AndAnotherClass {
                Task task;
            }

            """);
        project = document5.Project;
        var combiner = new Combiner();
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
            A.Fake<IConsole>(o => o.Strict()),
            A.Fake<IInteraction>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        // Act
        var result = await combiner.CombineAsync(project, new[] { document1, document2, document3, document4, document5 }, context);

        // Assert
        Assert.That(result, Is.Not.Null);
        var syntaxTree = await result.GetSyntaxTreeAsync();
        Assert.That(syntaxTree, Is.Not.Null);
        var root = await syntaxTree!.GetRootAsync();
        Assert.That(root, Is.Not.Null);
        var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>().Select(u => u.Name?.ToString()).ToList();
        Assert.That(usingDirectives, Has.Count.EqualTo(5));
        Assert.That(usingDirectives, Is.EquivalentTo(new[] { "System", "System.Collections.Generic", "System.Linq", "System.Text", "System.Threading.Tasks" }));
    }

    static (Project project, Document[] documents) CreateProject(params string[] sources)
    {
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp)
            .WithMetadataReferences(GetCoreReferences())
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var documents = new Document[sources.Length];
        for (var i = 0; i < sources.Length; i++)
        {
            var document = project.AddDocument($"TestDocument{i + 1}", sources[i]);
            project = document.Project;
            documents[i] = document;
        }

        // Re-fetch documents from the final project so they all share the same solution snapshot.
        for (var i = 0; i < documents.Length; i++)
            documents[i] = project.GetDocument(documents[i].Id)!;

        return (project, documents);
    }

    static PackContext CreatePackContext()
    {
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
        return new PackContext(
            parameters,
            A.Fake<IConsole>(o => o.Strict()),
            A.Fake<IInteraction>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );
    }

    static async Task<Document> RunFlattenAndCombineAsync(PackContext context, Project project, Document[] documents)
    {
        var deleteNamespaces = new DeleteNamespaces();
        for (var i = 0; i < documents.Length; i++)
        {
            documents[i] = await deleteNamespaces.ProcessAsync(documents[i], context);
            project = documents[i].Project;
        }
        for (var i = 0; i < documents.Length; i++)
            documents[i] = project.GetDocument(documents[i].Id)!;

        return await new Combiner().CombineAsync(project, documents, context);
    }

    [Test]
    public async Task CombineAsync_WithUsingOfSelfNamespaceInSameFile_DropsTheDanglingUsing()
    {
        // Reproduction for issue #61 (the single-file shape from the report) but driven through the
        // full flatten+combine pipeline rather than DeleteNamespaces alone, to confirm the *output*
        // compiles. `using MyApp;` sits in the same file as `namespace MyApp`.
        var context = CreatePackContext();
        var (project, documents) = CreateProject(
            """
            using System;
            using MyApp;
            namespace MyApp
            {
                class Program
                {
                }
            }
            """);

        var result = await RunFlattenAndCombineAsync(context, project, documents);

        var root = await result.GetSyntaxRootAsync();
        var usings = root!.DescendantNodes().OfType<UsingDirectiveSyntax>().Select(u => u.Name?.ToString()).ToArray();
        var compilation = await result.Project.GetCompilationAsync();
        var cs0246 = compilation!.GetDiagnostics().Where(d => d.Id == "CS0246").ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(usings, Does.Not.Contain("MyApp"), "The dangling `using MyApp;` should have been removed.");
            Assert.That(cs0246, Is.Empty, "The combined script should not have any unresolved namespace errors.");
        });
    }

    [Test]
    public async Task CombineAsync_WithUsingOfSelfNamespaceInFileThatDoesNotDeclareIt_DropsTheDanglingUsing()
    {
        // Reproduction for issue #61 across multiple files. The `using MyApp;` lives in a file that
        // does NOT itself declare `namespace MyApp` (the namespace is declared in the other files).
        // The per-file DeleteNamespaces cleanup cannot see that, so the combine step must drop the
        // now-dangling using; otherwise the intermediate script fails to compile with CS0246.
        var context = CreatePackContext();
        var (project, documents) = CreateProject(
            // Program.cs: declares the namespace, references Helper from the same logical namespace.
            """
            using System;
            namespace MyApp
            {
                class Program
                {
                    Helper _helper = new Helper();
                }
            }
            """,
            // Helper.cs: declares the namespace too.
            """
            namespace MyApp
            {
                class Helper
                {
                }
            }
            """,
            // Extensions.cs: imports the self-namespace but does NOT declare it.
            """
            using System;
            using MyApp;
            static class Extensions
            {
            }
            """);

        var result = await RunFlattenAndCombineAsync(context, project, documents);

        var root = await result.GetSyntaxRootAsync();
        var usings = root!.DescendantNodes().OfType<UsingDirectiveSyntax>().Select(u => u.Name?.ToString()).ToArray();
        var compilation = await result.Project.GetCompilationAsync();
        var cs0246 = compilation!.GetDiagnostics().Where(d => d.Id == "CS0246").ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(usings, Does.Not.Contain("MyApp"), "The dangling `using MyApp;` should have been removed.");
            Assert.That(cs0246, Is.Empty, "The combined script should not have any unresolved namespace errors.");
        });
    }

    [Test]
    public async Task CombineAsync_WithPreservedMember_KeepsPreserveAnnotations()
    {
        var workspace = new AdhocWorkspace();

        var project = workspace.AddProject("TestProject", LanguageNames.CSharp)
            .WithMetadataReferences(GetCoreReferences())
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var document = project.AddDocument("TestDocument",
            """
            enum PreservedEnum
            {
                Alpha
            }
            """);

        var root = await document.GetSyntaxRootAsync();
        var enumDeclaration = root!.DescendantNodes().OfType<EnumDeclarationSyntax>().Single();
        document = document.WithSyntaxRoot(root.ReplaceNode(enumDeclaration,
            enumDeclaration.WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"))));

        project = document.Project;
        var combiner = new Combiner();
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
            A.Fake<IConsole>(o => o.Strict()),
            A.Fake<IInteraction>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        var result = await combiner.CombineAsync(project, [document], context);
        var combinedRoot = await result.GetSyntaxRootAsync();
        var combinedEnum = combinedRoot!.DescendantNodes().OfType<EnumDeclarationSyntax>().Single();

        Assert.That(combinedEnum.ShouldBePreserved(), Is.True);
    }
}
