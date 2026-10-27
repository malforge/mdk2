using Mdk.Hub.Features.Updates;

namespace Mdk.Hub.Tests.Features.Updates;

[TestFixture]
public class UpdateManagerSdkParsingTests
{
    [Test]
    public void ParseBestSdkVersion_EmptyOutput_ReturnsNull()
    {
        var result = UpdateManager.ParseBestSdkVersion("");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseBestSdkVersion_OnlyDotNet10_ReturnsThatVersion()
    {
        var output = "10.0.100 [/usr/share/dotnet/sdk]";

        var result = UpdateManager.ParseBestSdkVersion(output);

        Assert.That(result, Is.EqualTo("10.0.100"));
    }

    [Test]
    public void ParseBestSdkVersion_MultipleDotNet10Versions_ReturnsHighest()
    {
        var output = """
            10.0.100 [/usr/share/dotnet/sdk]
            10.0.103 [/usr/share/dotnet/sdk]
            """;

        var result = UpdateManager.ParseBestSdkVersion(output);

        Assert.That(result, Is.EqualTo("10.0.103"));
    }

    [Test]
    public void ParseBestSdkVersion_OnlyDotNet8_ReturnsNull()
    {
        var output = "8.0.404 [/usr/share/dotnet/sdk]";

        var result = UpdateManager.ParseBestSdkVersion(output);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseBestSdkVersion_SingleDotNet9_ReturnsThatVersion()
    {
        var output = "9.0.101 [/usr/share/dotnet/sdk]";

        var result = UpdateManager.ParseBestSdkVersion(output);

        Assert.That(result, Is.EqualTo("9.0.101"));
    }

    [Test]
    public void ParseBestSdkVersion_DotNet9AndDotNet10_ReturnsDotNet10Version()
    {
        var output = """
            9.0.101 [/usr/share/dotnet/sdk]
            10.0.100 [/usr/share/dotnet/sdk]
            """;

        var result = UpdateManager.ParseBestSdkVersion(output);

        Assert.That(result, Is.EqualTo("10.0.100"));
    }

    [Test]
    public void ParseBestSdkVersion_OnlyNewerMajorThanDotNet10_ReturnsNull()
    {
        var output = "11.0.100 [/usr/share/dotnet/sdk]";

        var result = UpdateManager.ParseBestSdkVersion(output);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseBestSdkVersion_DotNet10AndNewerMajor_ReturnsDotNet10Version()
    {
        var output = """
            10.0.100 [/usr/share/dotnet/sdk]
            11.0.100 [/usr/share/dotnet/sdk]
            """;

        var result = UpdateManager.ParseBestSdkVersion(output);

        Assert.That(result, Is.EqualTo("10.0.100"));
    }

    [Test]
    public void ParseBestSdkVersion_MultipleDotNet9Versions_ReturnsHighest()
    {
        var output = """
            9.0.100 [/usr/share/dotnet/sdk]
            9.0.101 [/usr/share/dotnet/sdk]
            9.0.103 [/usr/share/dotnet/sdk]
            """;

        var result = UpdateManager.ParseBestSdkVersion(output);

        Assert.That(result, Is.EqualTo("9.0.103"));
    }

    [Test]
    public void ParseBestSdkVersion_WindowsStyleLineEndings_ParsesCorrectly()
    {
        // Windows uses \r\n line endings
        var output = "9.0.101 [C:\\Program Files\\dotnet\\sdk]\r\n10.0.100 [C:\\Program Files\\dotnet\\sdk]\r\n";

        var result = UpdateManager.ParseBestSdkVersion(output);

        Assert.That(result, Is.EqualTo("10.0.100"));
    }

    [Test]
    public void ParseBestSdkVersion_MixedVersions_ReturnsHighestSupported()
    {
        var output = """
            8.0.404 [/usr/share/dotnet/sdk]
            9.0.100 [/usr/share/dotnet/sdk]
            9.0.103 [/usr/share/dotnet/sdk]
            10.0.103 [/usr/share/dotnet/sdk]
            """;

        var result = UpdateManager.ParseBestSdkVersion(output);

        Assert.That(result, Is.EqualTo("10.0.103"));
    }

    [Test]
    public void ParseBestSdkVersion_WhitespaceOnly_ReturnsNull()
    {
        var result = UpdateManager.ParseBestSdkVersion("   \n   \r\n   ");

        Assert.That(result, Is.Null);
    }
}
