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
        Assert.That(result, Is.True);
        Assert.That(version.ToString(), Is.EqualTo("1.0.0"));
    }

    [Test]
    public void TryParse_WithValidVersionAndPreRelease_ShouldReturnTrue()
    {
        var result = SemanticVersion.TryParse("1.0.0-alpha", out var version);
        Console.WriteLine(version);
        Assert.That(result, Is.True);
        Assert.That(version.ToString(), Is.EqualTo("1.0.0-alpha"));
    }

    [Test]
    public void TryParse_WithValidVersionAndPreReleaseAndBuildMetadata_ShouldReturnTrue()
    {
        var result = SemanticVersion.TryParse("1.0.0-alpha+build", out var version);
        Console.WriteLine(version);
        Assert.That(result, Is.True);
        Assert.That(version.ToString(), Is.EqualTo("1.0.0-alpha+build"));
    }

    [Test]
    public void TryParse_WithInvalidVersion_ShouldReturnFalse()
    {
        var result = SemanticVersion.TryParse("1.0", out var version);
        Console.WriteLine(version);
        Assert.That(result, Is.False);
    }

    [Test]
    public void CompareTo_SameVersion_ReturnsZero()
    {
        var version1 = new SemanticVersion(1, 0, 0);
        var version2 = new SemanticVersion(1, 0, 0);

        Assert.That(version1.CompareTo(version2), Is.EqualTo(0));
    }
    
    [Test]
    public void CompareTo_PrereleaseIsLowerThanNormal()
    {
        var normalVersion = new SemanticVersion(1, 0, 0);
        var prereleaseVersion = new SemanticVersion(1, 0, 0, "alpha");

        Assert.That(normalVersion.CompareTo(prereleaseVersion), Is.GreaterThan(0));
        Assert.That(prereleaseVersion.CompareTo(normalVersion), Is.LessThan(0));
    }

    [Test]
    public void CompareTo_PrereleaseNumericIdentifiersAreComparedNumerically()
    {
        var version1 = new SemanticVersion(1, 0, 0, "alpha.1");
        var version2 = new SemanticVersion(1, 0, 0, "alpha.2");

        Assert.That(version1.CompareTo(version2), Is.LessThan(0));
        Assert.That(version2.CompareTo(version1), Is.GreaterThan(0));
    }

    [Test]
    public void CompareTo_PrereleaseNonNumericIdentifiersAreComparedLexically()
    {
        var version1 = new SemanticVersion(1, 0, 0, "alpha.beta");
        var version2 = new SemanticVersion(1, 0, 0, "alpha.gamma");

        Assert.That(version1.CompareTo(version2), Is.LessThan(0));
        Assert.That(version2.CompareTo(version1), Is.GreaterThan(0));
    }
    
    [Test]
    public void TryParse_WithStarVersion_ShouldReturnTrue()
    {
        var result = SemanticVersion.TryParse("*", out var version);
        Console.WriteLine(version);
        
        Assert.That(result, Is.True);
        Assert.That(version.ToString(), Is.EqualTo("*"));
        Assert.That(version.Wildcard, Is.True);
    }
    
    [Test]
    public void CompareTo_WithStarMinorVersion_ShouldReturnTrue()
    {
        var result = SemanticVersion.TryParse("2.*", out var version);
        Console.WriteLine(version);
        Assert.That(result, Is.True);
        Assert.That(version.ToString(), Is.EqualTo("2.*"));
        Assert.That(version.Major, Is.EqualTo(2));
        Assert.That(version.Minor, Is.EqualTo(-1));
        Assert.That(version.Patch, Is.EqualTo(-1));
        Assert.That(version.Wildcard, Is.True);
    }
    
    [Test]
    public void CompareTo_WithStarPatchVersion_ShouldReturnTrue()
    {
        var result = SemanticVersion.TryParse("2.1.*", out var version);
        Console.WriteLine(version);
        
        Assert.That(result, Is.True);
        Assert.That(version.ToString(), Is.EqualTo("2.1.*"));
        Assert.That(version.Major, Is.EqualTo(2));
        Assert.That(version.Minor, Is.EqualTo(1));
        Assert.That(version.Patch, Is.EqualTo(-1));
        Assert.That(version.Wildcard, Is.True);
    }
    
    [Test]
    public void CompareTo_WithStarVersion_ShouldCompareMinVersion()
    {
        var version1 = new SemanticVersion(1, 0, 0);
        var version2 = new SemanticVersion(2, 0, 0);
        var version3 = new SemanticVersion(2, 1, 0);
        var version4 = new SemanticVersion(2, 1, 1);
        
        var starVersion = new SemanticVersion(2, -1, -1, wildcard: true);
        
        Assert.That(version1.CompareTo(starVersion), Is.LessThan(0));
        Assert.That(version2.CompareTo(starVersion), Is.EqualTo(0));
        Assert.That(version3.CompareTo(starVersion), Is.EqualTo(0));
        Assert.That(version4.CompareTo(starVersion), Is.EqualTo(0));
    }
}