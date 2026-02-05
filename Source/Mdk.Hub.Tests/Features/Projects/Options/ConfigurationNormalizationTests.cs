using Mdk.Hub.Features.Projects.Configuration;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Utility;
using NUnit.Framework;

namespace Mdk.Hub.Tests.Features.Projects.Options;

[TestFixture]
public class ConfigurationNormalizationTests
{
    [Test]
    public void Normalization_MovesInteractiveFromMainToLocal()
    {
        // Arrange - Main has interactive (wrong place)
        var mainIni = new Ini()
            .WithSection("mdk")
            .WithKey("mdk", "type", "programmableblock")
            .WithKey("mdk", "interactive", "OpenHub");

        var localIni = new Ini();

        // Act - Remove from main, add to local
        mainIni = mainIni.WithoutKey("mdk", "interactive");
        localIni = localIni.WithSection("mdk").WithKey("mdk", "interactive", "OpenHub");

        // Assert
        Assert.That(mainIni["mdk"]["interactive"].IsEmpty(), Is.True, "interactive removed from main");
        Assert.That(localIni["mdk"]["interactive"].Value, Is.EqualTo("OpenHub"), "interactive moved to local");
    }

    [Test]
    public void Normalization_MovesOutputFromMainToLocal_PreservesComment()
    {
        // Arrange - Main has output with a comment
        var mainText = @"[mdk]
type=programmableblock

; User's custom output location
output=C:\MyScripts
";
        Ini.TryParse(mainText, out var mainIni);
        var localIni = new Ini();

        // Act - Move the key WITH its comment
        var outputKey = mainIni["mdk"]["output"];
        mainIni = mainIni.WithoutKey("mdk", "output");
        localIni = localIni.WithSection("mdk").WithKey("mdk", outputKey);

        // Assert
        Assert.That(mainIni["mdk"]["output"].IsEmpty(), Is.True, "output removed from main");
        Assert.That(localIni["mdk"]["output"].Value, Is.EqualTo("C:\\MyScripts"), "output moved to local");
        Assert.That(localIni["mdk"]["output"].Comment, Does.Contain("; User's custom output location"), 
            "Comment moved with the key");
    }

    [Test]
    public void Normalization_PreservesCustomKeysInMain()
    {
        // Arrange - Main has standard keys + custom keys
        var mainIni = new Ini()
            .WithSection("mdk")
            .WithKey("mdk", "type", "programmableblock")
            .WithKey("mdk", "mycustomkey", "customvalue")
            .WithKey("mdk", "output", "bin");  // Should move to local

        // Act - Remove only known local keys
        mainIni = mainIni.WithoutKey("mdk", "output");

        // Assert
        Assert.That(mainIni["mdk"]["type"].Value, Is.EqualTo("programmableblock"), "type preserved");
        Assert.That(mainIni["mdk"]["mycustomkey"].Value, Is.EqualTo("customvalue"), "custom key preserved");
        Assert.That(mainIni["mdk"]["output"].IsEmpty(), Is.True, "output removed");
    }

    [Test]
    public void Normalization_PreservesCustomSections()
    {
        // Arrange - Main has custom section
        var mainIni = new Ini()
            .WithSection("mdk")
            .WithKey("mdk", "type", "programmableblock")
            .WithSection("deployment")
            .WithKey("deployment", "server", "192.168.1.100");

        // Act - Remove misplaced local keys (none in this case)
        mainIni = mainIni.WithoutKey("mdk", "output");

        // Assert
        Assert.That(mainIni["deployment"]["server"].Value, Is.EqualTo("192.168.1.100"), 
            "Custom section preserved");
    }

    [Test]
    public void Normalization_ComplexScenario_MovesKeysPreservesComments()
    {
        // Arrange - The messy INI from the user
        var mainText = @"; Toggle trace (on|off) (verbose output)
[mdk]

; A list of allowed namespaces
namespaces=IngameScript
; This is a programmable block script project
type=programmableblock

ignores=obj/**/*,MDK/**/*,**/*.debug.cs
trace=off
minify=full
interactive=OpenHub
";
        Ini.TryParse(mainText, out var mainIni);
        var localIni = new Ini();

        // Act - Move interactive to local WITH its position (no comment in this case)
        var interactiveKey = mainIni["mdk"]["interactive"];
        mainIni = mainIni.WithoutKey("mdk", "interactive");
        localIni = localIni.WithSection("mdk").WithKey("mdk", interactiveKey);

        // Assert - Main keeps everything except interactive
        Assert.That(mainIni["mdk"]["type"].Value, Is.EqualTo("programmableblock"));
        Assert.That(mainIni["mdk"]["namespaces"].Value, Is.EqualTo("IngameScript"));
        Assert.That(mainIni["mdk"]["interactive"].IsEmpty(), Is.True, "interactive removed from main");
        
        // Local gets interactive
        Assert.That(localIni["mdk"]["interactive"].Value, Is.EqualTo("OpenHub"));
        
        // Original structure/comments preserved in main
        var mainOutput = mainIni.ToString();
        Assert.That(mainOutput, Does.Contain("; Toggle trace"), "Section comment preserved");
        Assert.That(mainOutput, Does.Contain("; A list of allowed namespaces"), "Key comment preserved");
        Assert.That(mainOutput, Does.Contain("; This is a programmable block script project"), "Type comment preserved");
    }
}
