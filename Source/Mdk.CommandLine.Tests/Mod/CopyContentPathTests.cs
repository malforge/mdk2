using System;
using System.IO;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.Mod.Pack.Jobs;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.Mod;

/// <summary>
/// Covers <see cref="CopyContentJob.ResolveContentRelativePath" /> - where a content file is written,
/// relative to the mod output directory. The first group locks the EXISTING behaviour (local content)
/// so the linked-content fix can't regress it; the later groups cover content from outside the project
/// (shared resources in a referenced shared project / submodule, or a NuGet package).
/// </summary>
[TestFixture]
public class CopyContentPathTests
{
    static readonly char S = Path.DirectorySeparatorChar;

    string _root = null!;
    DirectoryInfo _project = null!;

    // Platform-correct path from segments, and the same with the leading ".\" the packer uses.
    static string P(params string[] parts) => string.Join(S, parts);
    static string Dot(params string[] parts) => $".{S}{P(parts)}";

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "mdk-content-path-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _project = new DirectoryInfo(Path.Combine(_root, "TestMod"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    string PathUnderRoot(params string[] parts) => Path.Combine(_root, P(parts));

    string CreateFile(params string[] parts)
    {
        var path = PathUnderRoot(parts);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "");
        return path;
    }

    // A project directory on a drive letter other than the temp folder's, so no relative path exists between them.
    static DirectoryInfo ProjectOnAnotherDrive()
    {
        var tempDrive = char.ToUpperInvariant(Path.GetPathRoot(Path.GetTempPath())![0]);
        var drive = "QRSTUVWXYZ".First(letter => letter != tempDrive);
        return new DirectoryInfo($@"{drive}:\Mods\TestMod");
    }

    // ── Existing behaviour: local content. Must be byte-for-byte unchanged by the fix. ──

    [Test]
    public void LocalContentUnderContentFolder_StripsContentPrefix()
    {
        var result = CopyContentJob.ResolveContentRelativePath(
            PathUnderRoot("TestMod", "Content", "Data", "EntityComponents.sbc"), _project,
            ["Content", "Data"], "EntityComponents.sbc");

        Assert.That(result, Is.EqualTo(Dot("Data", "EntityComponents.sbc")));
    }

    [Test]
    public void LocalContentUnderNestedContentFolder_StripsContentPrefix()
    {
        var result = CopyContentJob.ResolveContentRelativePath(
            PathUnderRoot("TestMod", "Content", "Textures", "Sprites", "Logistics_Gear.dds"), _project,
            ["Content", "Textures", "Sprites"], "Logistics_Gear.dds");

        Assert.That(result, Is.EqualTo(Dot("Textures", "Sprites", "Logistics_Gear.dds")));
    }

    [Test]
    public void LocalContentAtProjectRoot_KeptAsIs()
    {
        var result = CopyContentJob.ResolveContentRelativePath(PathUnderRoot("TestMod", "thumb.png"), _project, [], "thumb.png");

        Assert.That(result, Is.EqualTo(Dot("thumb.png")));
    }

    // ── Content linked in from OUTSIDE the project (escaping "..\" path). ──

    [Test]
    public void LinkedContentFromOutsideProject_UsesLogicalLinkPath()
    {
        // <AdditionalFiles Include="..\Mal.MdkModMixin.Ion\Content\Data\LCDTextures.sbc"
        //                  Link="Content\Data\LCDTextures.sbc" />
        var result = CopyContentJob.ResolveContentRelativePath(
            PathUnderRoot("Mal.MdkModMixin.Ion", "Content", "Data", "LCDTextures.sbc"), _project,
            ["Content", "Data"], "LCDTextures.sbc");

        Assert.That(result, Is.EqualTo(Dot("Data", "LCDTextures.sbc")));
    }

    [Test]
    public void LinkedNestedContentFromOutsideProject_UsesLogicalLinkPath()
    {
        var result = CopyContentJob.ResolveContentRelativePath(
            PathUnderRoot("Mal.MdkModMixin.Ion", "Content", "Textures", "Sprites", "Ion_Gear.dds"), _project,
            ["Content", "Textures", "Sprites"], "Ion_Gear.dds");

        Assert.That(result, Is.EqualTo(Dot("Textures", "Sprites", "Ion_Gear.dds")));
    }

    [Test]
    public void ContentFromOutsideProjectWithoutLinkInSharedProject_UsesPathInsideSharedProject()
    {
        // A NuGet package on the same drive as the project, reached through a long "..\" path.
        CreateFile("packages", "mal.test.ion", "1.0.0", "contentFiles", "cs", "any", "Shared", "Mal.Test.Ion.projitems");
        var file = CreateFile("packages", "mal.test.ion", "1.0.0", "contentFiles", "cs", "any", "Shared", "Content", "Data", "LCDTextures.sbc");

        var result = CopyContentJob.ResolveContentRelativePath(file, _project, [], "LCDTextures.sbc");

        Assert.That(result, Is.EqualTo(Dot("Data", "LCDTextures.sbc")));
    }

    [Test]
    public void OutsideProjectWithoutLinkOrSharedProject_FallsBackToOriginalBehaviour()
    {
        var result = CopyContentJob.ResolveContentRelativePath(PathUnderRoot("Other", "Content", "Data", "Y.sbc"), _project, [], "Y.sbc");

        Assert.That(result, Is.EqualTo(Dot("..", "Other", "Content", "Data", "Y.sbc")));
    }

    // ── Content on another drive from the project: the relative path is rooted. ──

    [Test, Platform("Win")]
    public void ContentOnAnotherDriveWithLink_UsesLogicalLinkPath()
    {
        var file = CreateFile("Shared", "Content", "Data", "LCDTextures.sbc");

        var result = CopyContentJob.ResolveContentRelativePath(file, ProjectOnAnotherDrive(), ["Content", "Data"], "LCDTextures.sbc");

        Assert.That(result, Is.EqualTo(Dot("Data", "LCDTextures.sbc")));
    }

    [Test, Platform("Win")]
    public void ContentOnAnotherDriveWithoutLink_UsesPathInsideSharedProject()
    {
        CreateFile("Shared", "Mal.Test.Ion.projitems");
        var file = CreateFile("Shared", "Content", "Data", "LCDTextures.sbc");

        var result = CopyContentJob.ResolveContentRelativePath(file, ProjectOnAnotherDrive(), [], "LCDTextures.sbc");

        Assert.That(result, Is.EqualTo(Dot("Data", "LCDTextures.sbc")));
    }

    [Test, Platform("Win")]
    public void ContentOnAnotherDriveWithoutLinkOrSharedProject_Throws()
    {
        var file = CreateFile("Loose", "Content", "Data", "LCDTextures.sbc");

        Assert.That(() => CopyContentJob.ResolveContentRelativePath(file, ProjectOnAnotherDrive(), [], "LCDTextures.sbc"), Throws.TypeOf<CommandLineException>());
    }
}
