using Mdk.CommandLine.Mod.Pack.Jobs;
using Mdk.CommandLine.Utility;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.Mod;

[TestFixture]
public class ScriptPathTests
{
    static readonly char S = Path.DirectorySeparatorChar;

    string _root = null!;

    static string P(params string[] parts) => string.Join(S, parts);
    static string Dot(params string[] parts) => $".{S}{P(parts)}";

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "mdk-script-path-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    string CreateFile(params string[] parts)
    {
        var path = Path.Combine(_root, P(parts));
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

    [Test]
    public void ResolveScriptRelativePath_ScriptInProject_KeepsItsPath()
    {
        var project = new DirectoryInfo(Path.Combine(_root, "TestMod"));
        var script = CreateFile("TestMod", "Blocks", "Door.cs");

        Assert.That(ProcessScriptsJob.ResolveScriptRelativePath(script, project), Is.EqualTo(Dot("Blocks", "Door.cs")));
    }

    [Test]
    public void ResolveScriptRelativePath_OutsideProjectWithoutSharedProject_KeepsItsPath()
    {
        var project = new DirectoryInfo(Path.Combine(_root, "TestMod"));
        var script = CreateFile("Lumel", "Element.cs");

        Assert.That(ProcessScriptsJob.ResolveScriptRelativePath(script, project), Is.EqualTo(P("..", "Lumel", "Element.cs")));
    }

    [Test]
    public void ResolveScriptRelativePath_SiblingSharedProjectOnSameDrive_UsesSharedProjectName()
    {
        var project = new DirectoryInfo(Path.Combine(_root, "TestMod"));
        CreateFile("Lumel", "Lumel.projitems");
        var script = CreateFile("Lumel", "Element.cs");

        Assert.That(ProcessScriptsJob.ResolveScriptRelativePath(script, project), Is.EqualTo(Dot("Lumel", "Element.cs")));
    }

    [Test]
    public void ResolveScriptRelativePath_PackageSharedProjectOnSameDrive_UsesSharedProjectName()
    {
        var project = new DirectoryInfo(Path.Combine(_root, "Mods", "TestMod"));
        CreateFile("packages", "mal.test.network", "1.0.0", "contentFiles", "cs", "any", "Shared", "Mal.Test.Network.projitems");
        var script = CreateFile("packages", "mal.test.network", "1.0.0", "contentFiles", "cs", "any", "Shared", "ByteData.cs");

        Assert.That(ProcessScriptsJob.ResolveScriptRelativePath(script, project), Is.EqualTo(Dot("Mal.Test.Network", "ByteData.cs")));
    }

    [Test, Platform("Win")]
    public void ResolveScriptRelativePath_SharedProjectOnAnotherDrive_UsesSharedProjectName()
    {
        CreateFile("packages", "mal.test.network", "1.0.0", "contentFiles", "cs", "any", "Shared", "Mal.Test.Network.projitems");
        var script = CreateFile("packages", "mal.test.network", "1.0.0", "contentFiles", "cs", "any", "Shared", "ByteData.cs");

        Assert.That(ProcessScriptsJob.ResolveScriptRelativePath(script, ProjectOnAnotherDrive()), Is.EqualTo(Dot("Mal.Test.Network", "ByteData.cs")));
    }

    [Test, Platform("Win")]
    public void ResolveScriptRelativePath_NestedScriptOnAnotherDrive_KeepsItsPathInsideSharedProject()
    {
        CreateFile("Shared", "Mal.Test.Network.projitems");
        var script = CreateFile("Shared", "Channels", "NetworkChannel.cs");

        Assert.That(ProcessScriptsJob.ResolveScriptRelativePath(script, ProjectOnAnotherDrive()), Is.EqualTo(Dot("Mal.Test.Network", "Channels", "NetworkChannel.cs")));
    }

    [Test, Platform("Win")]
    public void ResolveScriptRelativePath_FolderWithTwoSharedProjects_DoesNotLookFurtherUp()
    {
        CreateFile("Outer.projitems");
        CreateFile("Shared", "First.projitems");
        CreateFile("Shared", "Second.projitems");
        var script = CreateFile("Shared", "ByteData.cs");

        Assert.That(ProcessScriptsJob.ResolveScriptRelativePath(script, ProjectOnAnotherDrive()), Is.EqualTo(Dot("Shared", "ByteData.cs")));
    }

    [Test, Platform("Win")]
    public void ResolveScriptRelativePath_ScriptOnAnotherDriveWithoutSharedProject_UsesItsFolderName()
    {
        var script = CreateFile("Loose", "Extras.cs");

        Assert.That(ProcessScriptsJob.ResolveScriptRelativePath(script, ProjectOnAnotherDrive()), Is.EqualTo(Dot("Loose", "Extras.cs")));
    }

    [Test, Platform("Win")]
    public void ResolveScriptRelativePath_ScriptAtTheRootOfAnotherDrive_UsesItsFileName()
    {
        var tempDrive = char.ToUpperInvariant(Path.GetPathRoot(Path.GetTempPath())![0]);
        var drives = "QRSTUVWXYZ".Where(letter => letter != tempDrive).Take(2).ToArray();
        var project = new DirectoryInfo($@"{drives[0]}:\Mods\TestMod");

        Assert.That(ProcessScriptsJob.ResolveScriptRelativePath($@"{drives[1]}:\Extras.cs", project), Is.EqualTo(Dot("Extras.cs")));
    }

    [Test]
    public void ResolveScriptRelativePath_ProjectFolderStartingWithTwoDots_KeepsItsPath()
    {
        var project = new DirectoryInfo(Path.Combine(_root, "TestMod"));
        CreateFile("TestMod", "..shared", "Inner.projitems");
        var script = CreateFile("TestMod", "..shared", "Element.cs");

        Assert.That(ProcessScriptsJob.ResolveScriptRelativePath(script, project), Is.EqualTo(P("..shared", "Element.cs")));
    }

    [Test]
    public void ResolveScriptRelativePath_SharedProjectAboveTheProject_IsIgnored()
    {
        var project = new DirectoryInfo(Path.Combine(_root, "Mods", "TestMod"));
        CreateFile("Common.projitems");
        var script = CreateFile("Other", "Util.cs");

        Assert.That(ProcessScriptsJob.ResolveScriptRelativePath(script, project), Is.EqualTo(P("..", "..", "Other", "Util.cs")));
    }

    [TestCase(@".\Data\X.cs", false)]
    [TestCase(@"Data\X.cs", false)]
    [TestCase(@"..shared\X.cs", false)]
    [TestCase(@"..\Lumel\X.cs", true)]
    [TestCase(@".\..\Lumel\X.cs", true)]
    public void LeavesBase_RelativePath_IsTrueOnlyWhenItStartsWithParentSegment(string path, bool expected)
    {
        Assert.That(PathEx.LeavesBase(path.Replace('\\', S)), Is.EqualTo(expected));
    }

    [Test]
    public void CombineInside_PathLeavingTheBase_IsFoldedInside()
    {
        Assert.That(PathEx.CombineInside(_root, P("..", "Lumel", "Element.cs")), Is.EqualTo(Path.Combine(_root, "Lumel", "Element.cs")));
    }

    [Test]
    public void CombineInside_RootedPath_Throws()
    {
        var rooted = Path.Combine(Path.GetTempPath(), "Element.cs");

        Assert.That(() => PathEx.CombineInside(_root, rooted), Throws.ArgumentException);
    }
}
