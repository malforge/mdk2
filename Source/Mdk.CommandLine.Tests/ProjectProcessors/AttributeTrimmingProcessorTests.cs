using System.Collections.Immutable;
using FakeItEasy;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.Shared;
using Mdk.CommandLine.Shared.Api;
using Mdk.CommandLine.Shared.AttributeTrimming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ProjectProcessors;

[TestFixture]
public class AttributeTrimmingProcessorTests
{
    [Test]
    public async Task ProcessWithResultAsync_TrimsSourceDefinedAttributeApplicationAndDeclaration()
    {
        var project = CreateProject("""
            using System;

            [Marker]
            public partial class Settings
            {
                public int Value => 42;
            }

            internal sealed class MarkerAttribute : Attribute
            {
            }
            """);

        var result = await ProcessAsync(project);
        var text = await GetDocumentTextAsync(result.Project);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(text, Does.Contain("public partial class Settings"));
        Assert.That(text, Does.Contain("Value => 42"));
        Assert.That(text, Does.Not.Contain("Marker"));
        Assert.That(text, Does.Not.Contain("Attribute"));
    }

    [Test]
    public async Task ProcessWithResultAsync_PreservesExternalAttributeInMixedList()
    {
        var project = CreateProject("""
            using System;

            [Obsolete, Marker]
            public class Settings
            {
            }

            internal sealed class MarkerAttribute : Attribute
            {
            }
            """);

        var result = await ProcessAsync(project);
        var text = await GetDocumentTextAsync(result.Project);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(text, Does.Contain("[Obsolete]"));
        Assert.That(text, Does.Not.Contain("Marker"));
        Assert.That(text, Does.Not.Contain("MarkerAttribute"));
    }

    [Test]
    public async Task ProcessWithResultAsync_PreservesExternalAttribute()
    {
        var project = CreateProject("""
            using System;

            [Obsolete]
            public class Settings
            {
            }
            """);

        var result = await ProcessAsync(project);
        var text = await GetDocumentTextAsync(result.Project);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(text, Does.Contain("Obsolete"));
        Assert.That(text, Does.Contain("public class Settings"));
    }

    [Test]
    public async Task ProcessWithResultAsync_IgnoresUnresolvedAttribute()
    {
        var project = CreateProject("""
            [DoesNotExist]
            public class Settings
            {
            }
            """);

        var result = await ProcessAsync(project);
        var text = await GetDocumentTextAsync(result.Project);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(text, Does.Contain("DoesNotExist"));
    }

    [Test]
    public async Task ProcessWithResultAsync_RemovesAliasUsedOnlyByTrimmedAttribute()
    {
        var project = CreateProject("""
            using System;
            using Marker = MarkerAttribute;

            [Marker]
            public class Settings
            {
            }

            internal sealed class MarkerAttribute : Attribute
            {
            }
            """);

        var result = await ProcessAsync(project);
        var text = await GetDocumentTextAsync(result.Project);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(text, Does.Not.Contain("using Marker"));
        Assert.That(text, Does.Not.Contain("MarkerAttribute"));
    }

    [Test]
    public async Task ProcessWithResultAsync_RemovesTrimmedTypeImportFromOtherwiseUntouchedDocument()
    {
        var project = CreateProject(
            ("Program.cs", """
                using Marker = MarkerAttribute;

                public class Settings
                {
                }
                """),
            ("Marker.cs", """
                using System;

                internal sealed class MarkerAttribute : Attribute
                {
                }
                """));

        var result = await ProcessAsync(project);
        var programText = await GetDocumentTextAsync(result.Project, "Program.cs");
        var markerText = await GetDocumentTextAsync(result.Project, "Marker.cs");

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(programText, Does.Not.Contain("using Marker"));
        Assert.That(programText, Does.Contain("public class Settings"));
        Assert.That(markerText, Does.Not.Contain("MarkerAttribute"));
    }

    [Test]
    public async Task ProcessWithResultAsync_RemovesNamespaceScopedAliasToTrimmedAttribute()
    {
        var project = CreateProject("""
            using System;

            namespace Example
            {
                using Marker = MarkerAttribute;

                [Marker]
                public class Settings
                {
                }

                internal sealed class MarkerAttribute : Attribute
                {
                }
            }
            """);

        var result = await ProcessAsync(project);
        var text = await GetDocumentTextAsync(result.Project);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(text, Does.Not.Contain("using Marker"));
        Assert.That(text, Does.Not.Contain("MarkerAttribute"));
        Assert.That(text, Does.Contain("public class Settings"));
    }

    [Test]
    public async Task ProcessWithResultAsync_RemovesStaticImportOfTrimmedAttribute()
    {
        var project = CreateProject("""
            using System;
            using static MarkerAttribute;

            [Marker(Value)]
            public class Settings
            {
            }

            internal sealed class MarkerAttribute : Attribute
            {
                public const int Value = 1;

                public MarkerAttribute(int value)
                {
                }
            }
            """);

        var result = await ProcessAsync(project);
        var text = await GetDocumentTextAsync(result.Project);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(text, Does.Not.Contain("using static MarkerAttribute"));
        Assert.That(text, Does.Not.Contain("MarkerAttribute"));
        Assert.That(text, Does.Contain("public class Settings"));
    }

    [Test]
    public async Task ProcessWithResultAsync_PreservesStaticImportOfExternalType()
    {
        var project = CreateProject("""
            using System;
            using static System.Math;

            [Marker]
            public class Settings
            {
                public double Value => Abs(-1);
            }

            internal sealed class MarkerAttribute : Attribute
            {
            }
            """);

        var result = await ProcessAsync(project);
        var text = await GetDocumentTextAsync(result.Project);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(text, Does.Contain("using static System.Math"));
        Assert.That(text, Does.Contain("Abs(-1)"));
    }

    [Test]
    public async Task ProcessWithResultAsync_PreservesNamespaceImportBecauseNamespaceIsNotTrimmed()
    {
        var project = CreateProject("""
            using System;
            using Tooling;

            [Marker]
            public class Settings
            {
            }

            namespace Tooling
            {
                internal sealed class MarkerAttribute : Attribute
                {
                }
            }
            """);

        var result = await ProcessAsync(project);
        var text = await GetDocumentTextAsync(result.Project);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(text, Does.Contain("using Tooling"));
        Assert.That(text, Does.Contain("namespace Tooling"));
        Assert.That(text, Does.Not.Contain("MarkerAttribute"));
    }

    [Test]
    public async Task ProcessWithResultAsync_PreservesNamespaceImportWhenScopeStillExists()
    {
        var project = CreateProject("""
            using System;
            using Tooling;

            [Marker]
            public class Settings
            {
                public Helper CreateHelper() => new Helper();
            }

            namespace Tooling
            {
                internal sealed class MarkerAttribute : Attribute
                {
                }

                public sealed class Helper
                {
                }
            }
            """);

        var result = await ProcessAsync(project);
        var text = await GetDocumentTextAsync(result.Project);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(text, Does.Contain("using Tooling"));
        Assert.That(text, Does.Contain("public sealed class Helper"));
        Assert.That(text, Does.Not.Contain("MarkerAttribute"));
    }

    [Test]
    public async Task ProcessWithResultAsync_PreservesUnrelatedImportsUsedImplicitly()
    {
        var project = CreateProject("""
            using System;
            using System.Collections.Generic;
            using System.Linq;

            [Marker]
            public class Settings
            {
                public IEnumerable<int> Select(IEnumerable<int> values) =>
                    from value in values
                    select value;
            }

            internal sealed class MarkerAttribute : Attribute
            {
            }
            """);

        var result = await ProcessAsync(project);
        var text = await GetDocumentTextAsync(result.Project);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(text, Does.Contain("using System.Linq"));
        Assert.That(text, Does.Contain("from value in values"));
    }

    [Test]
    public async Task ProcessWithResultAsync_PreservesBalancedPreprocessorDirectives()
    {
        var project = CreateProject("""
            using System;

            #if TOOLING
            [Marker]
            #endif
            public class Settings
            {
            }

            #if TOOLING
            internal sealed class MarkerAttribute : Attribute
            {
            }
            #endif
            """);

        var result = await ProcessAsync(project);
        var text = await GetDocumentTextAsync(result.Project);
        var syntaxTree = CSharpSyntaxTree.ParseText(
            text,
            new CSharpParseOptions(LanguageVersion.CSharp6).WithPreprocessorSymbols("TOOLING"));

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(syntaxTree.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error), Is.Empty);
        Assert.That(text, Does.Contain("#if TOOLING"));
        Assert.That(text, Does.Contain("#endif"));
        Assert.That(text.Split("#if TOOLING").Length, Is.EqualTo(text.Split("#endif").Length));
        Assert.That(text, Does.Not.Contain("Marker"));
        Assert.That(text, Does.Contain("public class Settings"));
    }

    [Test]
    public async Task ProcessWithResultAsync_PreservesCommentsAroundRemovedNodes()
    {
        var project = CreateProject("""
            using System;

            // Tooling-only marker.
            [Marker]
            // Runtime settings.
            public class Settings
            {
            }

            // Tooling-only declaration.
            internal sealed class MarkerAttribute : Attribute
            {
            }
            """);

        var result = await ProcessAsync(project);
        var text = await GetDocumentTextAsync(result.Project);

        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(text, Does.Contain("// Runtime settings."));
        Assert.That(text, Does.Contain("public class Settings"));
    }

    [Test]
    public async Task ProcessWithResultAsync_DiagnosesRuntimeUseOfSourceDefinedAttribute()
    {
        var project = CreateProject("""
            using System;

            public class Settings
            {
                public Type AttributeType => typeof(MarkerAttribute);
            }

            internal sealed class MarkerAttribute : Attribute
            {
            }
            """);

        var result = await ProcessAsync(project);

        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Id), Does.Contain("MDK04"));
    }

    static async Task<AttributeTrimmingResult> ProcessAsync(Project project)
    {
        var processor = new AttributeTrimmingProcessor();
        return await processor.ProcessWithResultAsync(project, CreateContext());
    }

    static Project CreateProject(string source)
    {
        return CreateProject(("Program.cs", source));
    }

    static Project CreateProject(params (string Name, string Source)[] documents)
    {
        var workspace = new AdhocWorkspace();
        var projectInfo = ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Default,
            "TestProject",
            "TestProject",
            LanguageNames.CSharp,
            parseOptions: new CSharpParseOptions(LanguageVersion.CSharp6).WithPreprocessorSymbols("TOOLING"),
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var project = workspace.AddProject(projectInfo);
        foreach (var reference in GetTrustedPlatformReferences())
            project = project.AddMetadataReference(reference);

        foreach (var (name, source) in documents)
            project = project.AddDocument(name, source, filePath: $"/tmp/{name}").Project;

        return project;
    }

    static IEnumerable<MetadataReference> GetTrustedPlatformReferences()
    {
        var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (trustedPlatformAssemblies == null)
            yield break;

        foreach (var path in trustedPlatformAssemblies.Split(Path.PathSeparator))
            yield return MetadataReference.CreateFromFile(path);
    }

    static async Task<string> GetDocumentTextAsync(Project project)
    {
        var document = project.Documents.Single();
        return (await document.GetTextAsync()).ToString();
    }

    static async Task<string> GetDocumentTextAsync(Project project, string name)
    {
        var document = project.Documents.Single(document => document.Name == name);
        return (await document.GetTextAsync()).ToString();
    }

    static PackContext CreateContext()
    {
        return new PackContext(
            new Parameters { Verb = Verb.Pack },
            A.Fake<IConsole>(),
            A.Fake<IInteraction>(),
            A.Fake<IFileFilter>(),
            A.Fake<IFileFilter>(),
            A.Fake<IFileSystem>(),
            ImmutableHashSet<string>.Empty);
    }

}
