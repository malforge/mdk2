using Mdk.Hub.Features.Updates;

namespace Mdk.Hub.Tests.Features.Updates;

[TestFixture]
public class UpdateManagerSdkParsingTests
{
    [Test]
    public void ParseBestSdk9Version_EmptyOutput_ReturnsNull()
    {
        var result = UpdateManager.ParseBestSdk9Version("");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseBestSdk9Version_OnlyDotNet10_ReturnsNull()
    {
        var output = "10.0.100 [/usr/share/dotnet/sdk]";

        var result = UpdateManager.ParseBestSdk9Version(output);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseBestSdk9Version_MultipleDotNet10Versions_ReturnsNull()
    {
        var output = """
            10.0.100 [/usr/share/dotnet/sdk]
            10.0.103 [/usr/share/dotnet/sdk]
            """;

        var result = UpdateManager.ParseBestSdk9Version(output);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseBestSdk9Version_OnlyDotNet8_ReturnsNull()
    {
        var output = "8.0.404 [/usr/share/dotnet/sdk]";

        var result = UpdateManager.ParseBestSdk9Version(output);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseBestSdk9Version_SingleDotNet9_ReturnsThatVersion()
    {
        var output = "9.0.101 [/usr/share/dotnet/sdk]";

        var result = UpdateManager.ParseBestSdk9Version(output);

        Assert.That(result, Is.EqualTo("9.0.101"));
    }

    [Test]
    public void ParseBestSdk9Version_DotNet9AndDotNet10_ReturnsDotNet9Version()
    {
        var output = """
            9.0.101 [/usr/share/dotnet/sdk]
            10.0.100 [/usr/share/dotnet/sdk]
            """;

        var result = UpdateManager.ParseBestSdk9Version(output);

        Assert.That(result, Is.EqualTo("9.0.101"));
    }

    [Test]
    public void ParseBestSdk9Version_MultipleDotNet9Versions_ReturnsHighest()
    {
        var output = """
            9.0.100 [/usr/share/dotnet/sdk]
            9.0.101 [/usr/share/dotnet/sdk]
            9.0.103 [/usr/share/dotnet/sdk]
            """;

        var result = UpdateManager.ParseBestSdk9Version(output);

        Assert.That(result, Is.EqualTo("9.0.103"));
    }

    [Test]
    public void ParseBestSdk9Version_WindowsStyleLineEndings_ParsesCorrectly()
    {
        // Windows uses \r\n line endings
        var output = "9.0.101 [C:\\Program Files\\dotnet\\sdk]\r\n10.0.100 [C:\\Program Files\\dotnet\\sdk]\r\n";

        var result = UpdateManager.ParseBestSdk9Version(output);

        Assert.That(result, Is.EqualTo("9.0.101"));
    }

    [Test]
    public void ParseBestSdk9Version_MixedVersions_ReturnsHighestDotNet9()
    {
        var output = """
            8.0.404 [/usr/share/dotnet/sdk]
            9.0.100 [/usr/share/dotnet/sdk]
            9.0.103 [/usr/share/dotnet/sdk]
            10.0.103 [/usr/share/dotnet/sdk]
            """;

        var result = UpdateManager.ParseBestSdk9Version(output);

        Assert.That(result, Is.EqualTo("9.0.103"));
    }

    [Test]
    public void ParseBestSdk9Version_WhitespaceOnly_ReturnsNull()
    {
        var result = UpdateManager.ParseBestSdk9Version("   \n   \r\n   ");

        Assert.That(result, Is.Null);
    }
}
