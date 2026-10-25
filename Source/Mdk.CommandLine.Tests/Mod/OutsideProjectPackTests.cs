using System.Collections.Immutable;
using FakeItEasy;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.Mod.Pack;
using Mdk.CommandLine.Mod.Pack.Jobs;
using Mdk.CommandLine.Shared;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.Mod;

/// <summary>
/// Runs the script and content jobs on files from outside the project, and checks what lands on disk.
/// </summary>
[TestFixture]
public class OutsideProjectPackTests
{
    string _root = null!;
    string _projectPath = null!;
    string _outputPath = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "mdk-outside-project-" + Guid.NewGuid().ToString("N"));
        _projectPath = Path.Combine(_root, "Mods", "TestMod");
        _outputPath = Path.Combine(_root, "Output", "TestMod");
        Directory.CreateDirectory(_projectPath);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    string CreateFile(string text, params string[] parts)
    {
        var path = Path.Combine(_root, Path.Combine(parts));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
        return path;
    }

    string[] FilesOutsideOutput() => Directory.GetFiles(_root, "*", SearchOption.AllDirectories)
        .Where(path => !path.StartsWith(_outputPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    string[] FilesInOutput() => Directory.GetFiles(_outputPath, "*", SearchOption.AllDirectories)
        .Select(path => Path.GetRelativePath(_outputPath, path))
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    ModPackContext CreateContext(Project project, ImmutableArray<Document> scripts, ImmutableArray<TextDocument> content)
    {
        var console = A.Fake<IConsole>();
        return new ModPackContext(
            new Parameters { Verb = Verb.Pack },
            console,
            A.Fake<IInteraction>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            new PackFileSystem(_projectPath, _outputPath, Path.Combine(_root, "Trace"), console),
            ImmutableHashSet<string>.Empty,
            project,
            scripts,
            content,
            new ProcessorSet<IDocumentProcessor>());
    }

    [Test]
    public async Task ProcessScriptsJob_ScriptsFromSharedProjectsOutsideTheProject_AreWrittenUnderTheSharedProjectName()
    {
        var session = CreateFile("class Session { }", "Mods", "TestMod", "Session.cs");
        CreateFile("", "Mods", "Lumel", "Lumel.projitems");
        var element = CreateFile("class Element { }", "Mods", "Lumel", "Controls", "Element.cs");
        CreateFile("", "packages", "mal.test.network", "1.0.0", "contentFiles", "cs", "any", "Shared", "Mal.Test.Network.projitems");
        var byteData = CreateFile("class ByteData { }", "packages", "mal.test.network", "1.0.0", "contentFiles", "cs", "any", "Shared", "ByteData.cs");
        var sourcesBefore = FilesOutsideOutput();

        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestMod", LanguageNames.CSharp);
        foreach (var path in new[] { session, element, byteData })
            project = project.AddDocument(Path.GetFileName(path), File.ReadAllText(path), filePath: path).Project;

        await new ProcessScriptsJob().ExecuteAsync(CreateContext(project, project.Documents.ToImmutableArray(), ImmutableArray<TextDocument>.Empty));

        var scripts = Path.Combine("Data", "Scripts", "TestMod");
        Assert.That(FilesInOutput(), Is.EquivalentTo(new[]
        {
            Path.Combine(scripts, "Session.cs"),
            Path.Combine(scripts, "Lumel", "Controls", "Element.cs"),
            Path.Combine(scripts, "Mal.Test.Network", "ByteData.cs")
        }));
        Assert.That(File.ReadAllText(Path.Combine(_outputPath, scripts, "Mal.Test.Network", "ByteData.cs")), Is.EqualTo("class ByteData { }"));
        Assert.That(FilesOutsideOutput(), Is.EqualTo(sourcesBefore));
    }

    [Test]
    public async Task CopyContentJob_ContentFromOutsideTheProject_IsCopiedInsideTheMod()
    {
        var blocks = CreateFile("blocks", "Mods", "TestMod", "Content", "Data", "Blocks.sbc");
        CreateFile("", "packages", "mal.test.ion", "1.0.0", "contentFiles", "cs", "any", "Shared", "Mal.Test.Ion.projitems");
        var textures = CreateFile("textures", "packages", "mal.test.ion", "1.0.0", "contentFiles", "cs", "any", "Shared", "Content", "Data", "LCDTextures.sbc");
        var loose = CreateFile("loose", "Mods", "Other", "Content", "Data", "Loose.sbc");
        var sourcesBefore = FilesOutsideOutput();

        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestMod", LanguageNames.CSharp);
        project = project.AddAdditionalDocument("Blocks.sbc", "blocks", ["Content", "Data"], blocks).Project;
        project = project.AddAdditionalDocument("LCDTextures.sbc", "textures", filePath: textures).Project;
        project = project.AddAdditionalDocument("Loose.sbc", "loose", filePath: loose).Project;

        await new CopyContentJob().ExecuteAsync(CreateContext(project, ImmutableArray<Document>.Empty, project.AdditionalDocuments.ToImmutableArray()));

        Assert.That(FilesInOutput(), Is.EquivalentTo(new[]
        {
            Path.Combine("Data", "Blocks.sbc"),
            Path.Combine("Data", "LCDTextures.sbc"),
            Path.Combine("Other", "Content", "Data", "Loose.sbc")
        }));
        Assert.That(File.ReadAllText(Path.Combine(_outputPath, "Data", "LCDTextures.sbc")), Is.EqualTo("textures"));
        Assert.That(FilesOutsideOutput(), Is.EqualTo(sourcesBefore));
    }
}
