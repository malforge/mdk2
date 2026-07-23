using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mdk2.References.Utility;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace MDK.CommandLine.Tests.RegressionTests;

/// <summary>
///     Ignores the test when a Space Engineers installation cannot be located. These tests pack a real
///     project, which requires compiling against the Space Engineers assemblies; without the game
///     installed (for example on CI) that compile cannot succeed, so the test is skipped rather than
///     reported as a failure.
/// </summary>
/// <remarks>
///     Detection mirrors how the product locates the game (Steam path from the registry, then a scan of
///     the Steam library folders for <c>SpaceEngineers\Bin64\SpaceEngineers.exe</c>). It intentionally
///     does not instantiate <c>SpaceEngineersFinder</c>, which is an MSBuild task; loading it would pull
///     the Microsoft.Build assemblies into the test process and collide with the packer's MSBuildLocator.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequiresSpaceEngineersAttribute : Attribute, ITestAction
{
    public void BeforeTest(ITest test)
    {
        if (!CanLocateSpaceEngineers())
            Assert.Ignore(
                "Ignored because a Space Engineers installation could not be located. These tests " +
                "compile a real project against the game assemblies, which are unavailable here.");
    }

    static bool CanLocateSpaceEngineers()
    {
        try
        {
            // RegistryReader throws (rather than returning null) when the Steam key is absent, as on a
            // bare CI runner - which is exactly the "no game" case, so treat any failure as not found.
            var steamPath = RegistryReader.GetSteamPath();
            if (string.IsNullOrEmpty(steamPath))
                return false;

            // Candidate library roots: the base Steam install plus every "path" in libraryfolders.vdf.
            var roots = new List<string> { steamPath };
            var vdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdf))
                roots.AddRange(Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s*\"([^\"]+)\"")
                    .Select(m => m.Groups[1].Value.Replace("\\\\", "\\")));

            return roots.Any(root => File.Exists(Path.Combine(
                root, "steamapps", "common", "SpaceEngineers", "Bin64", "SpaceEngineers.exe")));
        }
        catch
        {
            return false;
        }
    }

    public void AfterTest(ITest test)
    {
    }

    public ActionTargets Targets => ActionTargets.Test;
}
