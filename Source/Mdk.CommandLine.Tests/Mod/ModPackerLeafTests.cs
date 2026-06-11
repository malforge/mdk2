using System.Collections.Generic;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.Mod.Pack;
using Mdk.CommandLine.Shared;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.Mod;

[TestFixture]
public class ModPackerLeafTests
{
    static Dictionary<string, string> Macros(string project, string branch) => new()
    {
        ["$MDK_PROJECT$"] = project,
        ["$MDK_BRANCH$"] = branch
    };

    [Test]
    public void ResolveOutputLeaf_NoBranchPatterns_ReturnsProjectName()
    {
        var leaf = ModPacker.ResolveOutputLeaf("Mal.Logistics", "alpha", new Dictionary<string, string>(), Macros("Mal.Logistics", "alpha"));

        Assert.That(leaf, Is.EqualTo("Mal.Logistics"));
    }

    [Test]
    public void ResolveOutputLeaf_MappedBranch_AppliesPatternWithMacros()
    {
        var patterns = new Dictionary<string, string> { ["alpha"] = "$MDK_PROJECT$.Alpha" };

        var leaf = ModPacker.ResolveOutputLeaf("Mal.Logistics", "alpha", patterns, Macros("Mal.Logistics", "alpha"));

        Assert.That(leaf, Is.EqualTo("Mal.Logistics.Alpha"));
    }

    [Test]
    public void ResolveOutputLeaf_UnmappedBranch_ReturnsProjectName()
    {
        var patterns = new Dictionary<string, string> { ["alpha"] = "$MDK_PROJECT$.Alpha" };

        var leaf = ModPacker.ResolveOutputLeaf("Mal.Logistics", "master", patterns, Macros("Mal.Logistics", "master"));

        Assert.That(leaf, Is.EqualTo("Mal.Logistics"));
    }

    [Test]
    public void ResolveOutputLeaf_PatternsConfiguredButBranchNull_Throws()
    {
        var patterns = new Dictionary<string, string> { ["alpha"] = "$MDK_PROJECT$.Alpha" };

        Assert.That(
            () => ModPacker.ResolveOutputLeaf("Mal.Logistics", null, patterns, Macros("Mal.Logistics", "")),
            Throws.TypeOf<CommandLineException>());
    }

    [Test]
    public void ResolveOutputLeaf_BranchMacroWithSlash_IsSanitizedToSingleLeaf()
    {
        var patterns = new Dictionary<string, string> { ["feature/x"] = "$MDK_PROJECT$.$MDK_BRANCH$" };

        var leaf = ModPacker.ResolveOutputLeaf("Mal.Logistics", "feature/x", patterns, Macros("Mal.Logistics", "feature/x"));

        Assert.That(leaf, Is.EqualTo("Mal.Logistics.feature-x"));
    }

    [Test]
    public void MacroReplacer_SubstitutesProjectAndBranchMacros()
    {
        var result = MacroReplacer.Replace("$MDK_PROJECT$ on $MDK_BRANCH$", Macros("Mal.Logistics", "alpha"));

        Assert.That(result, Is.EqualTo("Mal.Logistics on alpha"));
    }
}
