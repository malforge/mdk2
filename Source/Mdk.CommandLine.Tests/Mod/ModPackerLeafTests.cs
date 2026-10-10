using System.Collections.Generic;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.Mod.Pack;
using Mdk.CommandLine.Mod.Pack.Jobs;
using Mdk.CommandLine.Shared;
using Mdk.CommandLine.Shared.Api;
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

    static Dictionary<string, BranchOutput> Branches(params (string branch, string pattern)[] entries)
    {
        var map = new Dictionary<string, BranchOutput>();
        foreach (var (branch, pattern) in entries)
            map[branch] = new BranchOutput(pattern);
        return map;
    }

    [Test]
    public void ResolveOutputLeaf_NoBranchOutputs_ReturnsProjectName()
    {
        var leaf = ModPacker.ResolveOutputLeaf("Mal.Logistics", "alpha", new Dictionary<string, BranchOutput>(), Macros("Mal.Logistics", "alpha"));

        Assert.That(leaf, Is.EqualTo("Mal.Logistics"));
    }

    [Test]
    public void ResolveOutputLeaf_MappedBranch_AppliesPatternWithMacros()
    {
        var leaf = ModPacker.ResolveOutputLeaf("Mal.Logistics", "alpha", Branches(("alpha", "$MDK_PROJECT$.Alpha")), Macros("Mal.Logistics", "alpha"));

        Assert.That(leaf, Is.EqualTo("Mal.Logistics.Alpha"));
    }

    [Test]
    public void ResolveOutputLeaf_UnmappedBranch_ReturnsProjectName()
    {
        var leaf = ModPacker.ResolveOutputLeaf("Mal.Logistics", "master", Branches(("alpha", "$MDK_PROJECT$.Alpha")), Macros("Mal.Logistics", "master"));

        Assert.That(leaf, Is.EqualTo("Mal.Logistics"));
    }

    [Test]
    public void ResolveOutputLeaf_OutputsConfiguredButBranchNull_Throws()
    {
        Assert.That(
            () => ModPacker.ResolveOutputLeaf("Mal.Logistics", null, Branches(("alpha", "$MDK_PROJECT$.Alpha")), Macros("Mal.Logistics", "")),
            Throws.TypeOf<CommandLineException>());
    }

    [Test]
    public void ResolveOutputLeaf_BranchMacroWithSlash_IsSanitizedToSingleLeaf()
    {
        var leaf = ModPacker.ResolveOutputLeaf("Mal.Logistics", "feature/x", Branches(("feature/x", "$MDK_PROJECT$.$MDK_BRANCH$")), Macros("Mal.Logistics", "feature/x"));

        Assert.That(leaf, Is.EqualTo("Mal.Logistics.feature-x"));
    }

    [Test]
    public void MacroReplacer_SubstitutesProjectAndBranchMacros()
    {
        var result = MacroReplacer.Replace("$MDK_PROJECT$ on $MDK_BRANCH$", Macros("Mal.Logistics", "alpha"));

        Assert.That(result, Is.EqualTo("Mal.Logistics on alpha"));
    }

    const string ModProjectFile = @"C:\Mods\Mal.Logistics\Mal.Logistics.csproj";

    [TestCase(@"C:\Mods\Mal.Logistics\mdk.ini")]
    [TestCase(@"C:\Mods\Mal.Logistics\mdk.local.ini")]
    [TestCase(@"C:\Mods\Mal.Logistics\Mal.Logistics.mdk.ini")]
    [TestCase(@"C:\Mods\Mal.Logistics\Mal.Logistics.mdk.local.ini")]
    [TestCase(@"C:\Mods\Mal.Logistics\MDK.INI")]
    [TestCase(@"C:\Mods\Mal.Logistics\.\Content\..\mdk.ini")]
    public void IsMdkConfiguration_ForAConfigurationFile_IsExcluded(string path)
    {
        Assert.That(FindAndFilterDocumentsJob.IsMdkConfiguration(path, FindAndFilterDocumentsJob.MdkConfigurationPaths(ModProjectFile)), Is.True);
    }

    [TestCase(@"C:\Mods\Mal.Logistics\Content\Data\Blocks.sbc")]
    [TestCase(@"C:\Mods\Mal.Logistics\Content\Data\settings.ini")]
    [TestCase(@"C:\Mods\Mal.Logistics\Content\mdk.ini")]
    [TestCase(@"C:\Mods\Mal.Logistics\thumb.png")]
    public void IsMdkConfiguration_ForModContent_IsKept(string path)
    {
        Assert.That(FindAndFilterDocumentsJob.IsMdkConfiguration(path, FindAndFilterDocumentsJob.MdkConfigurationPaths(ModProjectFile)), Is.False);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("Mal.Logistics.csproj")]
    public void MdkConfigurationPaths_WithoutAResolvableProjectDirectory_IsEmpty(string? projectFile)
    {
        Assert.That(FindAndFilterDocumentsJob.MdkConfigurationPaths(projectFile), Is.Empty);
    }

    [Test]
    public void IsMdkConfiguration_WithNoConfigurationPaths_KeepsEverything()
    {
        Assert.That(FindAndFilterDocumentsJob.IsMdkConfiguration(@"C:\Mods\Mal.Logistics\mdk.ini", FindAndFilterDocumentsJob.MdkConfigurationPaths(null)), Is.False);
    }
}
