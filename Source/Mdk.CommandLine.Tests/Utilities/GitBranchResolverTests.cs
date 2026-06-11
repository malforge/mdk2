using System;
using System.IO;
using Mdk.CommandLine.Utility;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.Utilities;

[TestFixture]
public class GitBranchResolverTests
{
    string _tempDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GitBranchResolverTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, true);
    }

    [Test]
    public void GetCurrentBranch_OnBranch_ReturnsBranchName()
    {
        WriteGitDir(_tempDirectory, "ref: refs/heads/alpha\n");

        Assert.That(GitBranchResolver.GetCurrentBranch(_tempDirectory), Is.EqualTo("alpha"));
    }

    [Test]
    public void GetCurrentBranch_BranchNameWithSlash_IsPreserved()
    {
        WriteGitDir(_tempDirectory, "ref: refs/heads/release/beta\n");

        Assert.That(GitBranchResolver.GetCurrentBranch(_tempDirectory), Is.EqualTo("release/beta"));
    }

    [Test]
    public void GetCurrentBranch_DetachedHead_ReturnsNull()
    {
        WriteGitDir(_tempDirectory, "0123456789abcdef0123456789abcdef01234567\n");

        Assert.That(GitBranchResolver.GetCurrentBranch(_tempDirectory), Is.Null);
    }

    [Test]
    public void GetCurrentBranch_FromNestedProjectDirectory_WalksUpToRepoRoot()
    {
        WriteGitDir(_tempDirectory, "ref: refs/heads/main\n");
        var nested = Path.Combine(_tempDirectory, "src", "MyMod");
        Directory.CreateDirectory(nested);

        Assert.That(GitBranchResolver.GetCurrentBranch(nested), Is.EqualTo("main"));
    }

    [Test]
    public void GetCurrentBranch_GitFilePointer_ResolvesToTargetGitDir()
    {
        // A worktree/submodule has a ".git" *file* containing "gitdir: <path>" rather than a directory.
        var realGitDir = Path.Combine(_tempDirectory, "actual-git");
        Directory.CreateDirectory(realGitDir);
        File.WriteAllText(Path.Combine(realGitDir, "HEAD"), "ref: refs/heads/worktree-branch\n");

        var workTree = Path.Combine(_tempDirectory, "worktree");
        Directory.CreateDirectory(workTree);
        File.WriteAllText(Path.Combine(workTree, ".git"), "gitdir: " + realGitDir + "\n");

        Assert.That(GitBranchResolver.GetCurrentBranch(workTree), Is.EqualTo("worktree-branch"));
    }

    [Test]
    public void GetCurrentBranch_NoGit_ReturnsNull()
    {
        Assert.That(GitBranchResolver.GetCurrentBranch(_tempDirectory), Is.Null);
    }

    [Test]
    public void GetCurrentBranch_NullOrEmptyStart_ReturnsNull()
    {
        Assert.That(GitBranchResolver.GetCurrentBranch(null), Is.Null);
        Assert.That(GitBranchResolver.GetCurrentBranch(string.Empty), Is.Null);
    }

    static void WriteGitDir(string root, string headContents)
    {
        var gitDir = Path.Combine(root, ".git");
        Directory.CreateDirectory(gitDir);
        File.WriteAllText(Path.Combine(gitDir, "HEAD"), headContents);
    }
}
