using System.Collections.Immutable;
using FakeItEasy;
using Mdk.CommandLine;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.Mod.Pack;
using Mdk.CommandLine.Mod.Pack.Jobs;
using Mdk.CommandLine.Shared;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.Mod;

[TestFixture]
public class ProcessScriptsJobTests
{
    const int DocumentCount = 40;

    string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "mdk-process-scripts-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    [Test]
    public async Task ExecuteAsync_WithTraceLoggedToFile_WritesEveryDocumentAndLogsInDocumentOrder()
    {
        var projectPath = Path.Combine(_root, "Project");
        var outputPath = Path.Combine(_root, "Output", "TestMod");
        var logPath = Path.Combine(_root, "pack.log");
        Directory.CreateDirectory(projectPath);

        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestMod", LanguageNames.CSharp);
        for (var i = 0; i < DocumentCount; i++)
            project = project.AddDocument($"Script{i:00}.cs", $"class Script{i:00} {{ }}", filePath: Path.Combine(projectPath, $"Script{i:00}.cs")).Project;
        var documents = project.Documents.OrderBy(d => d.Name).ToImmutableArray();

        var console = new FileConsole(logPath, true);
        var context = new ModPackContext(
            new Parameters { Verb = Verb.Pack },
            console,
            A.Fake<IInteraction>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            new PackFileSystem(projectPath, outputPath, Path.Combine(_root, "Trace"), console),
            ImmutableHashSet<string>.Empty,
            project,
            documents,
            ImmutableArray<TextDocument>.Empty,
            new ProcessorSet<IDocumentProcessor>(typeof(SlowerForEarlierDocumentsProcessor)));

        await new ProcessScriptsJob().ExecuteAsync(context);

        var scriptsDirectory = Path.Combine(outputPath, "Data", "Scripts", "TestMod");
        Assert.That(Directory.GetFiles(scriptsDirectory, "*.cs"), Has.Length.EqualTo(DocumentCount));

        var log = File.ReadAllText(logPath);
        var tracedOrder = documents.Select(d => log.IndexOf($"Traced {d.Name}", StringComparison.Ordinal)).ToArray();
        Assert.That(tracedOrder, Has.None.EqualTo(-1));
        Assert.That(tracedOrder, Is.Ordered);
    }

    public class SlowerForEarlierDocumentsProcessor : IDocumentProcessor
    {
        public async Task<Document> ProcessAsync(Document document, IPackContext context)
        {
            var index = int.Parse(document.Name.Substring("Script".Length, 2));
            await Task.Delay((DocumentCount - index) * 5);
            context.Console.Trace($"Traced {document.Name}");
            return document;
        }
    }
}
