using Mdk2.PbAnalyzers.Tests.Harness;
using NUnit.Framework;

namespace Mdk2.PbAnalyzers.Tests;

/// <summary>
///     Runs the new rules over the real scripts kept as CLI test data. Synthetic tests prove the rules fire where they
///     should; this proves they stay quiet on code that is known to work, which is the failure mode that would actually
///     reach people.
/// </summary>
/// <remarks>
///     Only MDK05 and MDK06 are asserted. The corpus trips plenty of MDK01, because these tests compile against stub
///     assemblies that carry a handful of types rather than the whole game, and against the running .NET rather than the
///     .NET Framework the whitelist is keyed to. That noise says nothing about the rules under test.
/// </remarks>
[TestFixture]
public class RealScriptCorpusTests
{
    [TestCase("AutomaticLCDs2MDK2")]
    [TestCase("Issue67")]
    [TestCase("Issue76")]
    [TestCase("Issue90")]
    [TestCase("Issue98")]
    [TestCase("LegacyScriptProject")]
    public void RealScript_ReportsNoUsingOrNamespaceProblems(string project)
    {
        var root = Path.Combine(FindSourceDirectory(), "Mdk.CommandLine.Tests", "TestData", project);

        // Not every fixture is in the repository - AutomaticLCDs2MDK2 is kept locally - so a missing one is ignored
        // rather than failed, the same way RequiresFileAttribute handles it over in the CLI tests.
        if (!Directory.Exists(root))
            Assert.Ignore($"corpus project not present: {root}");

        var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsUnderDirectory(file, "obj") && !IsUnderDirectory(file, "bin"))
            .Select(file => new SourceFile(Path.GetRelativePath(root, file), File.ReadAllText(file)))
            .ToList();

        if (files.Count == 0)
            Assert.Ignore($"corpus project has no sources: {root}");

        var result = PbAnalyzerRunner.Run(files);

        Assert.Multiple(() =>
        {
            Assert.That(result.OfRule("MDK05"), Is.Empty, $"{project} is known-good code:\n{result.Describe()}");
            Assert.That(result.OfRule("MDK06"), Is.Empty, $"{project} is known-good code:\n{result.Describe()}");
        });
    }

    static bool IsUnderDirectory(string path, string directoryName)
        => path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(segment => segment.Equals(directoryName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    ///     Walks up from the test assembly to the Source directory. Done by search rather than by counting parents,
    ///     because the number of levels depends on whether the build was platform-qualified (bin/Debug versus
    ///     bin/x64/Debug).
    /// </summary>
    static string FindSourceDirectory()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Mdk.CommandLine.Tests")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Source directory from " + TestContext.CurrentContext.TestDirectory);
    }
}
