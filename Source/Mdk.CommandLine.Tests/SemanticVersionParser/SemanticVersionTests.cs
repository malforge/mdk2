using FluentAssertions;
using Mdk.CommandLine;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.SemanticVersionParser;

[TestFixture]
public class SemanticVersionTests
{
    [Test]
    public void TryParse_WithValidVersion_ShouldReturnTrue()
    {
        var result = SemanticVersion.TryParse("1.0.0", out var version);
        Console.WriteLine(version);
        result.Should().BeTrue();
        version.ToString().Should().Be("1.0.0");
    }
    
    [Test]
    public void TryParse_WithValidVersionAndPreRelease_ShouldReturnTrue()
    {
        var result = SemanticVersion.TryParse("1.0.0-alpha", out var version);
        Console.WriteLine(version);
        result.Should().BeTrue();
        version.ToString().Should().Be("1.0.0-alpha");
    }
    
    [Test]
    public void TryParse_WithValidVersionAndPreReleaseAndBuildMetadata_ShouldReturnTrue()
    {
        var result = SemanticVersion.TryParse("1.0.0-alpha+build", out var version);
        Console.WriteLine(version);
        result.Should().BeTrue();
        version.ToString().Should().Be("1.0.0-alpha+build");
    }
    
    [Test]
    public void TryParse_WithInvalidVersion_ShouldReturnFalse()
    {
        var result = SemanticVersion.TryParse("1.0", out var version);
        Console.WriteLine(version);
        result.Should().BeFalse();
    }
}