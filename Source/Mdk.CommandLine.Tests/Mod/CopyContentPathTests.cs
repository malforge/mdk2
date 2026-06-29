using System;
using System.IO;
using Mdk.CommandLine.Mod.Pack.Jobs;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.Mod;

/// <summary>
/// Covers <see cref="CopyContentJob.ResolveContentRelativePath" /> - where a content file is written,
/// relative to the mod output directory. The first group locks the EXISTING behaviour (local content)
/// so the linked-content fix can't regress it; the second group covers content linked in from outside
/// the project (shared resources in a referenced shared project / submodule).
/// </summary>
[TestFixture]
public class CopyContentPathTests
{
    static readonly char S = Path.DirectorySeparatorChar;

    // Platform-correct path from segments, and the same with the leading ".\" the packer uses.
    static string P(params string[] parts) => string.Join(S, parts);
    static string Dot(params string[] parts) => $".{S}{P(parts)}";

    // ── Existing behaviour: local content. Must be byte-for-byte unchanged by the fix. ──

    [Test]
    public void LocalContentUnderContentFolder_StripsContentPrefix()
    {
        var result = CopyContentJob.ResolveContentRelativePath(
            Dot("Content", "Data", "EntityComponents.sbc"),
            ["Content", "Data"], "EntityComponents.sbc");

        Assert.That(result, Is.EqualTo(Dot("Data", "EntityComponents.sbc")));
    }

    [Test]
    public void LocalContentUnderContentFolder_WithoutDotPrefix_StillStrips()
    {
        var result = CopyContentJob.ResolveContentRelativePath(
            P("Content", "Textures", "Sprites", "Logistics_Gear.dds"),
            ["Content", "Textures", "Sprites"], "Logistics_Gear.dds");

        Assert.That(result, Is.EqualTo(Dot("Textures", "Sprites", "Logistics_Gear.dds")));
    }

    [Test]
    public void LocalContentAtProjectRoot_KeptAsIs()
    {
        var result = CopyContentJob.ResolveContentRelativePath(Dot("thumb.png"), [], "thumb.png");

        Assert.That(result, Is.EqualTo(Dot("thumb.png")));
    }

    // ── New behaviour: content linked in from OUTSIDE the project (escaping "..\" path). ──

    [Test]
    public void LinkedContentFromOutsideProject_UsesLogicalLinkPath()
    {
        // <AdditionalFiles Include="..\Mal.MdkModMixin.Ion\Content\Data\LCDTextures.sbc"
        //                  Link="Content\Data\LCDTextures.sbc" />
        var result = CopyContentJob.ResolveContentRelativePath(
            P("..", "Mal.MdkModMixin.Ion", "Content", "Data", "LCDTextures.sbc"),
            ["Content", "Data"], "LCDTextures.sbc");

        Assert.That(result, Is.EqualTo(Dot("Data", "LCDTextures.sbc")));
    }

    [Test]
    public void LinkedNestedContentFromOutsideProject_UsesLogicalLinkPath()
    {
        var result = CopyContentJob.ResolveContentRelativePath(
            P("..", "Mal.MdkModMixin.Ion", "Content", "Textures", "Sprites", "Ion_Gear.dds"),
            ["Content", "Textures", "Sprites"], "Ion_Gear.dds");

        Assert.That(result, Is.EqualTo(Dot("Textures", "Sprites", "Ion_Gear.dds")));
    }

    [Test]
    public void OutsideProjectWithoutLogicalPath_FallsBackToOriginalBehaviour()
    {
        // No Folders (no Link) => no substitution. Documents that the fix REQUIRES a Link, and that
        // nothing silently changes for a Link-less outside file (it keeps the historical result).
        var input = P("..", "Other", "Content", "Data", "Y.sbc");

        var result = CopyContentJob.ResolveContentRelativePath(input, [], "Y.sbc");

        Assert.That(result, Is.EqualTo($".{S}{input}"));
    }
}
