using System.Collections.Immutable;
using System.Linq;
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
public class CodeSmallifierTests : DocumentProcessorTests<CodeSmallifier>
{
    [Test]
    public async Task ProcessAsync_WhenFieldsAreUninitialized_CompactsByType()
    {
        const string testCode =
            """
            class Program
            {
                string A;
                string[] C;
                string B;
                int D;
                int E;
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
        var root = await result.GetSyntaxRootAsync();
        var programClass = root!.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
        var fields = programClass.Members.OfType<FieldDeclarationSyntax>().ToList();

        Assert.That(fields, Has.Count.EqualTo(3));
        Assert.That(fields[0].Declaration.Type.ToString(), Is.EqualTo("string"));
        Assert.That(fields[0].Declaration.Variables.Count, Is.EqualTo(2));
        Assert.That(fields[1].Declaration.Type.ToString(), Is.EqualTo("string[]"));
        Assert.That(fields[1].Declaration.Variables.Count, Is.EqualTo(1));
        Assert.That(fields[2].Declaration.Type.ToString(), Is.EqualTo("int"));
        Assert.That(fields[2].Declaration.Variables.Count, Is.EqualTo(2));
    }
}
