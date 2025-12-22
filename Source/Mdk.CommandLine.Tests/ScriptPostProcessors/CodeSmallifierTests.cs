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
                private static string A,B = "x";
                internal string C;
                string[] D;
                string E,H;
                int F,G = 1;
            }
            """;

        Assert.That(actual.ToString(), Is.EqualTo(expected));
    }
}
