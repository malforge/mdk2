using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace MDK.CommandLine.Tests.RegressionTests;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequiresFileAttribute(string filePath) : Attribute, ITestAction
{
    private readonly string _filePath = filePath;

    public void BeforeTest(ITest test)
    {
        if (!File.Exists(_filePath))
        {
            Assert.Ignore($"Test ignored because file '{_filePath}' does not exist.");
        }
    }

    public void AfterTest(ITest test)
    {
        // No action needed after the test
    }

    public ActionTargets Targets => ActionTargets.Test;
}