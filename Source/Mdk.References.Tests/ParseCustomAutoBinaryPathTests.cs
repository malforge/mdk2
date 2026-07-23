using Mdk2.References;

namespace Mdk.References.Tests;

[TestFixture]
public class ParseCustomAutoBinaryPathTests
{
    [Test]
    public void ReturnsValue_ForNormalPath()
    {
        const string json = """{ "CustomAutoBinaryPath": "/home/user/SE/Bin64" }""";
        Assert.That(SpaceEngineersFinder.ParseCustomAutoBinaryPath(json), Is.EqualTo("/home/user/SE/Bin64"));
    }

    [Test]
    public void ReturnsNull_ForAutoSentinel()
    {
        const string json = """{ "CustomAutoBinaryPath": "auto" }""";
        Assert.That(SpaceEngineersFinder.ParseCustomAutoBinaryPath(json), Is.Null);
    }

    [Test]
    public void ReturnsNull_WhenKeyMissing()
    {
        const string json = """{ "SomeOtherKey": "value" }""";
        Assert.That(SpaceEngineersFinder.ParseCustomAutoBinaryPath(json), Is.Null);
    }

    [Test]
    public void ReturnsNull_ForEmptyValue()
    {
        const string json = """{ "CustomAutoBinaryPath": "" }""";
        Assert.That(SpaceEngineersFinder.ParseCustomAutoBinaryPath(json), Is.Null);
    }

    [Test]
    public void ReturnsNull_ForWhitespaceValue()
    {
        const string json = """{ "CustomAutoBinaryPath": "   " }""";
        Assert.That(SpaceEngineersFinder.ParseCustomAutoBinaryPath(json), Is.Null);
    }

    [Test]
    public void ReturnsNull_ForNullJson()
    {
        Assert.That(SpaceEngineersFinder.ParseCustomAutoBinaryPath(null!), Is.Null);
    }

    [Test]
    public void ReturnsNull_ForEmptyJson()
    {
        Assert.That(SpaceEngineersFinder.ParseCustomAutoBinaryPath(""), Is.Null);
    }

    [Test]
    public void ToleratesFormatting_SpacesAndNewlinesAroundColon()
    {
        // The Hub writes indented, multi-line JSON; the key/value may be spread across whitespace.
        const string json =
            """
            {
                "CustomAutoBinaryPath"    :
                    "/opt/se/Bin64"
            }
            """;
        Assert.That(SpaceEngineersFinder.ParseCustomAutoBinaryPath(json), Is.EqualTo("/opt/se/Bin64"));
    }

    [Test]
    public void ReturnsValue_FromRealisticMultiKeySettings()
    {
        // Representative of the actual %APPDATA%/MDK2/settings.json layout, where the key sits
        // among several others.
        const string json =
            """
            {
                "Theme": "Dark",
                "CheckForUpdates": true,
                "CustomAutoBinaryPath": "/home/user/.steam/steamapps/common/SpaceEngineers/Bin64",
                "LastProject": "MyScript"
            }
            """;
        Assert.That(
            SpaceEngineersFinder.ParseCustomAutoBinaryPath(json),
            Is.EqualTo("/home/user/.steam/steamapps/common/SpaceEngineers/Bin64"));
    }

    [Test]
    public void ReturnsBackslashesVerbatim_KnownUnescapingLimitation()
    {
        // KNOWN LIMITATION (characterization test, not endorsement): the parser does a raw substring
        // and does NOT JSON-unescape the value. A Windows path stored as "C:\\SE\\Bin64" therefore
        // comes back with the backslashes doubled. Windows path APIs tolerate the doubled separators,
        // so it works in practice, but if the parser is ever switched to a real JSON reader this test
        // should be updated to expect the unescaped "C:\SE\Bin64".
        const string json = """{ "CustomAutoBinaryPath": "C:\\SE\\Bin64" }""";
        Assert.That(SpaceEngineersFinder.ParseCustomAutoBinaryPath(json), Is.EqualTo(@"C:\\SE\\Bin64"));
    }
}
