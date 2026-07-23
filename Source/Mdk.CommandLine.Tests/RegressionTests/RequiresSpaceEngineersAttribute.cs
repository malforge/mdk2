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
        if (!new SpaceEngineers().TryGetInstallPath("Bin64", out _))
            Assert.Ignore(
                "Ignored because a Space Engineers installation could not be located. These tests " +
                "compile a real project against the game assemblies, which are unavailable here.");
    }

    public void AfterTest(ITest test)
    {
    }

    public ActionTargets Targets => ActionTargets.Test;
}
