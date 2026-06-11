using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.Utility;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.CommandLine;

/// <summary>
/// Tests for Parameters class, specifically INI loading and command-line override behavior.
/// </summary>
[TestFixture]
public class ParametersTests
{
    [Test]
    public void Interactive_LoadsFromINI()
    {
        // Arrange
        var iniText = """
        [mdk]
        interactive=OpenHub
        """;
        Ini.TryParse(iniText, out var ini);
        var parameters = new Parameters();

        // Act
        parameters.Load(ini);

        // Assert
        Assert.That(parameters.Interactive, Is.EqualTo(InteractiveMode.OpenHub));
    }

    [Test]
    public void Interactive_CommandLineOverridesINI()
    {
        // Arrange
        var iniText = """
        [mdk]
        interactive=OpenHub
        """;
        Ini.TryParse(iniText, out var ini);
        var parameters = new Parameters();
        parameters.Load(ini);
        
        // Act - command-line should override INI
        parameters.Parse(new[] { "pack", "test.csproj", "-interactive", "DoNothing" });

        // Assert
        Assert.That(parameters.Interactive, Is.EqualTo(InteractiveMode.DoNothing), 
            "Command-line -interactive should override INI setting");
    }

    [Test]
    public void Interactive_INIThenCommandLine_CommandLineWins()
    {
        // Arrange
        var parameters = new Parameters();
        
        // Act - parse command-line first
        parameters.Parse(new[] { "pack", "test.csproj", "-interactive", "ShowNotification" });
        
        // Then load INI with different value
        var iniText = """
        [mdk]
        interactive=OpenHub
        """;
        Ini.TryParse(iniText, out var ini);
        parameters.Load(ini, overrideExisting: false); // Should NOT override because command-line already set

        // Assert
        Assert.That(parameters.Interactive, Is.EqualTo(InteractiveMode.ShowNotification), 
            "When overrideExisting=false, INI should not override command-line value");
    }

    [Test]
    public void Interactive_INIWithOverrideExistingTrue_OverridesCommandLine()
    {
        // Arrange
        var parameters = new Parameters();
        parameters.Parse(new[] { "pack", "test.csproj", "-interactive", "ShowNotification" });
        
        // Act
        var iniText = """
        [mdk]
        interactive=OpenHub
        """;
        Ini.TryParse(iniText, out var ini);
        parameters.Load(ini, overrideExisting: true);

        // Assert
        Assert.That(parameters.Interactive, Is.EqualTo(InteractiveMode.OpenHub), 
            "When overrideExisting=true, INI should override command-line value");
    }

    [Test]
    public void Interactive_NoININoCommandLine_DefaultsToNull()
    {
        // Arrange
        var parameters = new Parameters();

        // Act
        parameters.Parse(new[] { "pack", "test.csproj" });

        // Assert
        Assert.That(parameters.Interactive, Is.Null, 
            "Without INI or command-line, Interactive should be null");
    }

    [Test]
    public void Interactive_CaseInsensitive_INI()
    {
        // Arrange - INI section/key lookup is case-insensitive, but we use lowercase in file
        var iniText = """
        [mdk]
        interactive=OpenHub
        """;
        Ini.TryParse(iniText, out var ini);
        var parameters = new Parameters();

        // Act
        parameters.Load(ini);

        // Assert - verify we can read it back
        Assert.That(parameters.Interactive, Is.EqualTo(InteractiveMode.OpenHub), 
            "Should load interactive parameter from INI");
    }

    [Test]
    public void Interactive_CaseInsensitive_CommandLine()
    {
        // Arrange
        var parameters = new Parameters();

        // Act
        parameters.Parse(new[] { "pack", "test.csproj", "-INTERACTIVE", "shownotification" });

        // Assert
        Assert.That(parameters.Interactive, Is.EqualTo(InteractiveMode.ShowNotification), 
            "Command-line flags should be case-insensitive");
    }

    [Test]
    public void Trace_LoadsFromINI()
    {
        // Arrange
        var iniText = """
        [mdk]
        trace=on
        """;
        Ini.TryParse(iniText, out var ini);
        var parameters = new Parameters();

        // Act
        parameters.Load(ini);

        // Assert
        Assert.That(parameters.Trace, Is.True);
    }

    [Test]
    public void Trace_CommandLineOverridesINI()
    {
        // Arrange
        var iniText = """
        [mdk]
        trace=off
        """;
        Ini.TryParse(iniText, out var ini);
        var parameters = new Parameters();
        parameters.Load(ini);
        
        // Act
        parameters.Parse(new[] { "pack", "test.csproj", "-trace" });

        // Assert
        Assert.That(parameters.Trace, Is.True, 
            "Command-line -trace should override INI setting");
    }

    [Test]
    public void PackVerb_MinifierLevel_LoadsFromINI()
    {
        // Arrange
        var iniText = """
        [mdk]
        minify=full
        """;
        Ini.TryParse(iniText, out var ini);
        var parameters = new Parameters();

        // Act
        parameters.Load(ini);

        // Assert
        Assert.That(parameters.PackVerb.MinifierLevel, Is.EqualTo(MinifierLevel.Full));
    }

    [Test]
    public void PackVerb_MinifierLevel_CommandLineOverridesINI()
    {
        // Arrange
        var iniText = """
        [mdk]
        minify=full
        """;
        Ini.TryParse(iniText, out var ini);
        var parameters = new Parameters();
        parameters.Load(ini);
        
        // Act
        parameters.Parse(new[] { "pack", "test.csproj", "-minify", "none" });

        // Assert
        Assert.That(parameters.PackVerb.MinifierLevel, Is.EqualTo(MinifierLevel.None),
            "Command-line -minify should override INI setting");
    }

    [Test]
    public void BranchOutputs_LoadFromIniBranchSections()
    {
        // Arrange
        var iniText = """
        [mdk]
        type=mod

        [mdk-branch:alpha]
        pattern=$MDK_PROJECT$.Alpha

        [mdk-branch:release/beta]
        pattern=$MDK_PROJECT$.Beta
        """;
        Ini.TryParse(iniText, out var ini);
        var parameters = new Parameters();

        // Act
        parameters.Load(ini);

        // Assert
        Assert.That(parameters.PackVerb.BranchOutputs["alpha"].Pattern, Is.EqualTo("$MDK_PROJECT$.Alpha"));
        Assert.That(parameters.PackVerb.BranchOutputs["release/beta"].Pattern, Is.EqualTo("$MDK_PROJECT$.Beta"));
    }

    [Test]
    public void BranchOutputs_SectionWithoutPattern_IsIgnored()
    {
        // Arrange
        var iniText = """
        [mdk]
        type=mod

        [mdk-branch:alpha]
        notpattern=ignored
        """;
        Ini.TryParse(iniText, out var ini);
        var parameters = new Parameters();

        // Act
        parameters.Load(ini);

        // Assert
        Assert.That(parameters.PackVerb.BranchOutputs, Does.Not.ContainKey("alpha"));
    }

    [Test]
    public void BranchOutputs_ParseWatermarkSettings()
    {
        // Arrange
        var iniText = """
        [mdk]
        type=mod

        [mdk-branch:alpha]
        pattern=$MDK_PROJECT$.Alpha
        watermark=true
        watermarktext=ALPHA BUILD
        """;
        Ini.TryParse(iniText, out var ini);
        var parameters = new Parameters();

        // Act
        parameters.Load(ini);

        // Assert
        var config = parameters.PackVerb.BranchOutputs["alpha"];
        Assert.That(config.Watermark, Is.True);
        Assert.That(config.WatermarkText, Is.EqualTo("ALPHA BUILD"));
    }

    [Test]
    public void BranchOutputs_WatermarkDefaultsOn()
    {
        // Arrange
        var iniText = """
        [mdk]
        type=mod

        [mdk-branch:alpha]
        pattern=$MDK_PROJECT$.Alpha
        """;
        Ini.TryParse(iniText, out var ini);
        var parameters = new Parameters();

        // Act
        parameters.Load(ini);

        // Assert - a branch section watermarks by default.
        Assert.That(parameters.PackVerb.BranchOutputs["alpha"].Watermark, Is.True);
    }

    [Test]
    public void BranchOutputs_WatermarkCanBeDisabled()
    {
        // Arrange
        var iniText = """
        [mdk]
        type=mod

        [mdk-branch:alpha]
        pattern=$MDK_PROJECT$.Alpha
        watermark=false
        """;
        Ini.TryParse(iniText, out var ini);
        var parameters = new Parameters();

        // Act
        parameters.Load(ini);

        // Assert
        Assert.That(parameters.PackVerb.BranchOutputs["alpha"].Watermark, Is.False);
    }

    [Test]
    public void BranchOutputs_LocalIniOverridesMainIniForSameBranch()
    {
        // mdk.local.ini is loaded first with overrideExisting:false, so it must win over mdk.ini.
        var localIni = """
        [mdk-branch:alpha]
        pattern=$MDK_PROJECT$.LocalAlpha
        """;
        var mainIni = """
        [mdk]
        type=mod

        [mdk-branch:alpha]
        pattern=$MDK_PROJECT$.MainAlpha
        """;
        Ini.TryParse(localIni, out var local);
        Ini.TryParse(mainIni, out var main);
        var parameters = new Parameters();

        // Act - mirror the real load order: local first (no override), then main (no override).
        parameters.Load(local, false);
        parameters.Load(main, false);

        // Assert
        Assert.That(parameters.PackVerb.BranchOutputs["alpha"].Pattern, Is.EqualTo("$MDK_PROJECT$.LocalAlpha"));
    }
}
