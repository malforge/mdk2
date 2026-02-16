using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.Utility;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.CommandLine;

/// <summary>
/// Integration tests verifying the complete Interactive parameter flow:
/// INI file → CLI parameter → final behavior
/// </summary>
[TestFixture]
public class InteractiveParameterIntegrationTests
{
    [Test]
    public void Scenario_NoINI_NoCommandLine_DefaultsToNull()
    {
        // Arrange
        var parameters = new Parameters();

        // Act
        parameters.Parse(new[] { "pack", "test.csproj" });

        // Assert
        Assert.That(parameters.Interactive, Is.Null,
            "Without INI or CLI flag, Interactive should be null (Hub decides default)");
    }

    [Test]
    public void Scenario_INIOnly_UsesINIValue()
    {
        // Arrange
        var iniText = """
        [mdk]
        interactive=ShowNotification
        """;
        Ini.TryParse(iniText, out var ini);
        var parameters = new Parameters();

        // Act
        parameters.Parse(new[] { "pack", "test.csproj" });
        parameters.Load(ini);

        // Assert
        Assert.That(parameters.Interactive, Is.EqualTo(InteractiveMode.ShowNotification),
            "When only INI is set, use that value");
    }

    [Test]
    public void Scenario_MSBuildPassesDoNothing_OverridesINI()
    {
        // This simulates: <MdkInteractive>no</MdkInteractive> in project file
        // Which MSBuild converts to: -interactive DoNothing
        
        // Arrange
        var iniText = """
        [mdk]
        interactive=ShowNotification
        """;
        Ini.TryParse(iniText, out var ini);
        var parameters = new Parameters();

        // Act - MSBuild would pass -interactive DoNothing
        parameters.Parse(new[] { "pack", "test.csproj", "-interactive", "DoNothing" });
        parameters.Load(ini, overrideExisting: false); // CLI should win

        // Assert
        Assert.That(parameters.Interactive, Is.EqualTo(InteractiveMode.DoNothing),
            "MSBuild property should override INI file");
    }

    [Test]
    public void Scenario_UserCommandLine_OverridesEverything()
    {
        // User runs: mdk pack test.csproj -interactive OpenHub
        
        // Arrange
        var iniText = """
        [mdk]
        interactive=ShowNotification
        """;
        Ini.TryParse(iniText, out var ini);
        var parameters = new Parameters();

        // Act
        parameters.Parse(new[] { "pack", "test.csproj", "-interactive", "OpenHub" });
        parameters.Load(ini, overrideExisting: false);

        // Assert
        Assert.That(parameters.Interactive, Is.EqualTo(InteractiveMode.OpenHub),
            "User command-line should override INI");
    }

    [Test]
    public void Scenario_MSBuildProperty_PassesThroughAllValues()
    {
        // Verify all three valid values work
        var testCases = new[]
        {
            (InteractiveMode.DoNothing, InteractiveMode.DoNothing),    // <MdkInteractive>no</MdkInteractive> or DoNothing
            (InteractiveMode.OpenHub, InteractiveMode.OpenHub),        // <MdkInteractive>OpenHub</MdkInteractive>
            (InteractiveMode.ShowNotification, InteractiveMode.ShowNotification) // <MdkInteractive>ShowNotification</MdkInteractive>
        };

        foreach (var (input, expected) in testCases)
        {
            // Arrange
            var parameters = new Parameters();

            // Act - MSBuild passes the value
            parameters.Parse(new[] { "pack", "test.csproj", "-interactive", input.ToString() });

            // Assert
            Assert.That(parameters.Interactive, Is.EqualTo(expected),
                $"MSBuild property value '{input}' should pass through correctly");
        }
    }

    [Test]
    public void Scenario_CaseInsensitive_CommandLineValues()
    {
        // Arrange
        var parameters = new Parameters();

        // Act
        parameters.Parse(new[] { "pack", "test.csproj", "-interactive", "donothing" });

        // Assert
        Assert.That(parameters.Interactive, Is.EqualTo(InteractiveMode.DoNothing),
            "Values should be case-insensitive");
    }

    [Test]
    public void Scenario_RealWorld_CI_DisablesInteractive()
    {
        // Real-world scenario: CI/CD pipeline with <MdkInteractive>no</MdkInteractive>
        // MSBuild converts to: -interactive DoNothing
        
        // Arrange - Project has INI with ShowNotification
        var iniText = """
        [mdk]
        interactive=ShowNotification
        """;
        Ini.TryParse(iniText, out var ini);
        var parameters = new Parameters();

        // Act - MSBuild passes DoNothing from <MdkInteractive>no</MdkInteractive>
        parameters.Parse(new[] { "pack", "MyScript.csproj", "-interactive", "DoNothing" });
        parameters.Load(ini, overrideExisting: false);

        // Assert
        Assert.That(parameters.Interactive, Is.EqualTo(InteractiveMode.DoNothing),
            "CI builds should be able to disable Hub interaction via MSBuild property");
    }
}
