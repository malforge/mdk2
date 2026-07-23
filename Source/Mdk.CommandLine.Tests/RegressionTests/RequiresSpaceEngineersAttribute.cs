using Mdk2.References.Utility;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace MDK.CommandLine.Tests.RegressionTests;

/// <summary>
///     Ignores the test when a Space Engineers installation cannot be located. These tests pack a real
///     project, which requires compiling against the Space Engineers assemblies; without the game
///     installed (for example on CI) that compile cannot succeed, so the test is skipped rather than
///     reported as a failure. Uses the same locator the product uses so "installed" means exactly what
///     it means during a real build.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequiresSpaceEngineersAttribute : Attribute, ITestAction
{
    public void BeforeTest(ITest test)
    {
        bool located;
        try
        {
            located = new SpaceEngineers().TryGetInstallPath("Bin64", out _);
        }
        catch
        {
            // The locator can throw rather than return false when Steam itself is absent (e.g. it
            // fails to open the Steam registry key on a bare CI runner). Any probe failure means the
            // game is not locatable here.
            located = false;
        }

        if (!located)
            Assert.Ignore(
                "Ignored because a Space Engineers installation could not be located. These tests " +
                "compile a real project against the game assemblies, which are unavailable here.");
    }

    public void AfterTest(ITest test)
    {
    }

    public ActionTargets Targets => ActionTargets.Test;
}
