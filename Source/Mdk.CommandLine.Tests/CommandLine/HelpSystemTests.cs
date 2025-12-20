using FakeItEasy;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.Shared.Api;
using NUnit.Framework;
using System.Text;

namespace MDK.CommandLine.Tests.CommandLine;

/// <summary>
/// Regression tests for the help system.
/// These tests verify that the help command correctly shows help text for each verb.
/// </summary>
[TestFixture]
public class HelpSystemTests
{
    class TestConsole : IConsole
    {
        readonly StringBuilder _output = new();
        
        public bool TraceEnabled => false;
        
        public IConsole Trace(string? message = null, int wrapIndent = 4)
        {
            return this;
        }
        
        public IConsole Print(string? message = null, int wrapIndent = 4)
        {
            _output.AppendLine(message);
            return this;
        }
        
        public string GetOutput() => _output.ToString();
        
        public void Clear() => _output.Clear();
    }

    [Test]
    public void ShowHelp_WithHelpVerb_ShowsGeneralHelp()
    {
        // Arrange
        var parameters = new Parameters();
        parameters.Parse(new[] { "help" });
        var console = new TestConsole();

        // Act
        parameters.ShowHelp(console);
        var output = console.GetOutput();

        // Assert
        Assert.That(parameters.Verb, Is.EqualTo(Verb.Help), "Verb should be Help");
        Assert.That(parameters.HelpVerb.Verb, Is.EqualTo(Verb.None), "HelpVerb.Verb should be None (default)");
        Assert.That(output, Contains.Substring("Usage: mdk [options] <verb>"), "Should show general usage");
        Assert.That(output, Contains.Substring("Verbs:"), "Should list available verbs");
        Assert.That(output, Contains.Substring("pack"), "Should mention pack verb");
        Assert.That(output, Contains.Substring("restore"), "Should mention restore verb");
    }

    [Test]
    public void ShowHelp_WithHelpPack_ShowsPackHelp()
    {
        // Arrange
        var parameters = new Parameters();
        parameters.Parse(new[] { "help", "pack" });
        var console = new TestConsole();

        // Act
        parameters.ShowHelp(console);
        var output = console.GetOutput();

        // Assert
        Assert.That(parameters.Verb, Is.EqualTo(Verb.Help), "Verb should be Help");
        Assert.That(parameters.HelpVerb.Verb, Is.EqualTo(Verb.Pack), "HelpVerb.Verb should be Pack");
        Assert.That(output, Contains.Substring("Usage: mdk pack"), "Should show pack usage");
        Assert.That(output, Contains.Substring("minifier"), "Should mention minifier option");
        Assert.That(output, !Contains.Substring("Usage: mdk restore"), "Should NOT show restore usage");
    }

    [Test]
    public void ShowHelp_WithHelpRestore_ShowsRestoreHelp()
    {
        // Arrange
        var parameters = new Parameters();
        parameters.Parse(new[] { "help", "restore" });
        var console = new TestConsole();

        // Act
        parameters.ShowHelp(console);
        var output = console.GetOutput();

        // Assert
        Assert.That(parameters.Verb, Is.EqualTo(Verb.Help), "Verb should be Help");
        Assert.That(parameters.HelpVerb.Verb, Is.EqualTo(Verb.Restore), "HelpVerb.Verb should be Restore");
        Assert.That(output, Contains.Substring("Usage: mdk restore"), "Should show restore usage");
        Assert.That(output, Contains.Substring("compatibility"), "Should mention compatibility check");
        Assert.That(output, !Contains.Substring("Usage: mdk pack"), "Should NOT show pack usage");
    }

    [Test]
    public void Parse_HelpWithPackArgument_SetsCorrectValues()
    {
        // Arrange
        var parameters = new Parameters();

        // Act
        parameters.Parse(new[] { "help", "pack" });

        // Assert
        Assert.That(parameters.Verb, Is.EqualTo(Verb.Help), "Main verb should be Help");
        Assert.That(parameters.HelpVerb.Verb, Is.EqualTo(Verb.Pack), "HelpVerb should be Pack");
    }

    [Test]
    public void Parse_HelpWithRestoreArgument_SetsCorrectValues()
    {
        // Arrange
        var parameters = new Parameters();

        // Act
        parameters.Parse(new[] { "help", "restore" });

        // Assert
        Assert.That(parameters.Verb, Is.EqualTo(Verb.Help), "Main verb should be Help");
        Assert.That(parameters.HelpVerb.Verb, Is.EqualTo(Verb.Restore), "HelpVerb should be Restore");
    }

    [Test]
    public void Parse_HelpWithNoArgument_SetsHelpVerbToNone()
    {
        // Arrange
        var parameters = new Parameters();

        // Act
        parameters.Parse(new[] { "help" });

        // Assert
        Assert.That(parameters.Verb, Is.EqualTo(Verb.Help), "Main verb should be Help");
        Assert.That(parameters.HelpVerb.Verb, Is.EqualTo(Verb.None), "HelpVerb should be None when no argument given");
    }

    [Test]
    public void ShowHelp_WithPackVerbDirectly_DoesNotShowHelp()
    {
        // Arrange - User runs "mdk pack" without project file
        var parameters = new Parameters();
        try
        {
            parameters.Parse(new[] { "pack" }); // This will throw because no project file
        }
        catch (CommandLineException)
        {
            // Expected - pack requires a project file
        }

        var console = new TestConsole();

        // Act
        parameters.Verb = Verb.Pack; // Simulate the verb being set
        parameters.ShowHelp(console);
        var output = console.GetOutput();

        // Assert
        // When Verb is Pack (not Help), ShowHelp should do nothing (empty case statement)
        Assert.That(output, Is.Empty.Or.Contains("MDK v"), "Should show at most the version, then do nothing");
    }

    [Test]
    public void ShowHelp_WithInvalidHelpVerb_ShowsGeneralHelp()
    {
        // Arrange
        var parameters = new Parameters
        {
            Verb = Verb.Help
        };
        // HelpVerb.Verb defaults to None
        var console = new TestConsole();

        // Act
        parameters.ShowHelp(console);
        var output = console.GetOutput();

        // Assert
        Assert.That(output, Contains.Substring("Usage: mdk [options] <verb>"), "Should show general help for invalid/unknown help verb");
    }
}
