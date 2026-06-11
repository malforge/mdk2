using System;
using System.IO;

namespace Mdk.CommandLine.Utility;

/// <summary>
///     Determines the current git branch by reading <c>.git/HEAD</c> directly, without spawning git or
///     taking a dependency on a git library.
/// </summary>
public static class GitBranchResolver
{
    const string RefPrefix = "ref: refs/heads/";

    /// <summary>
    ///     Gets the name of the git branch currently checked out for the repository containing
    ///     <paramref name="startDirectory" />.
    /// </summary>
    /// <param name="startDirectory">A directory inside the working tree; the search walks up from here.</param>
    /// <returns>
    ///     The branch name (which may contain slashes, e.g. <c>feature/foo</c>), or <c>null</c> if no git
    ///     repository is found, the repository is in a detached HEAD state, or anything cannot be read.
    /// </returns>
    public static string? GetCurrentBranch(string? startDirectory)
    {
        try
        {
            var gitDir = FindGitDir(startDirectory);
            if (gitDir == null)
                return null;

            var headPath = Path.Combine(gitDir, "HEAD");
            if (!File.Exists(headPath))
                return null;

            var head = File.ReadAllText(headPath).Trim();
            if (head.StartsWith(RefPrefix, StringComparison.Ordinal))
            {
                var branch = head[RefPrefix.Length..].Trim();
                return string.IsNullOrEmpty(branch) ? null : branch;
            }

            // A raw commit hash (or anything else) means we're not on a branch (detached HEAD).
            return null;
        }
        catch
        {
            // If we can't read git state for any reason, behave as if there's no branch.
            return null;
        }
    }

    /// <summary>
    ///     Walks up from <paramref name="startDirectory" /> looking for a <c>.git</c> entry and resolves it to
    ///     the directory that contains <c>HEAD</c>. Handles both a normal <c>.git</c> directory and a
    ///     <c>.git</c> file (worktrees / submodules) that points elsewhere via <c>gitdir:</c>.
    /// </summary>
    static string? FindGitDir(string? startDirectory)
    {
        if (string.IsNullOrEmpty(startDirectory))
            return null;

        var current = new DirectoryInfo(startDirectory);
        while (current != null)
        {
            var gitDirPath = Path.Combine(current.FullName, ".git");

            if (Directory.Exists(gitDirPath))
                return gitDirPath;

            if (File.Exists(gitDirPath))
                return ResolveGitFile(gitDirPath, current.FullName);

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    ///     Resolves a <c>.git</c> file (used by worktrees and submodules) of the form
    ///     <c>gitdir: &lt;path&gt;</c> to the absolute directory it references.
    /// </summary>
    static string? ResolveGitFile(string gitFilePath, string containingDirectory)
    {
        const string gitDirPrefix = "gitdir:";
        foreach (var rawLine in File.ReadAllLines(gitFilePath))
        {
            var line = rawLine.Trim();
            if (!line.StartsWith(gitDirPrefix, StringComparison.Ordinal))
                continue;

            var target = line[gitDirPrefix.Length..].Trim();
            if (string.IsNullOrEmpty(target))
                return null;

            // The referenced path may be relative to the directory containing the .git file.
            if (!Path.IsPathRooted(target))
                target = Path.GetFullPath(Path.Combine(containingDirectory, target));

            return Directory.Exists(target) ? target : null;
        }

        return null;
    }
}
