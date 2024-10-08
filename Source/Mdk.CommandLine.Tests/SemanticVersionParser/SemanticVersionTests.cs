﻿using FluentAssertions;
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

    [Test]
    public void CompareTo_SameVersion_ReturnsZero()
    {
        var version1 = new SemanticVersion(1, 0, 0);
        var version2 = new SemanticVersion(1, 0, 0);

        version1.CompareTo(version2).Should().Be(0);
    }
    
    [Test]
    public void CompareTo_PrereleaseIsLowerThanNormal()
    {
        var normalVersion = new SemanticVersion(1, 0, 0);
        var prereleaseVersion = new SemanticVersion(1, 0, 0, "alpha");

        normalVersion.CompareTo(prereleaseVersion).Should().BeGreaterThan(0);
        prereleaseVersion.CompareTo(normalVersion).Should().BeLessThan(0);
    }

    [Test]
    public void CompareTo_PrereleaseNumericIdentifiersAreComparedNumerically()
    {
        var version1 = new SemanticVersion(1, 0, 0, "alpha.1");
        var version2 = new SemanticVersion(1, 0, 0, "alpha.2");

        version1.CompareTo(version2).Should().BeLessThan(0);
        version2.CompareTo(version1).Should().BeGreaterThan(0);
    }

    [Test]
    public void CompareTo_PrereleaseNonNumericIdentifiersAreComparedLexically()
    {
        var version1 = new SemanticVersion(1, 0, 0, "alpha.beta");
        var version2 = new SemanticVersion(1, 0, 0, "alpha.gamma");

        version1.CompareTo(version2).Should().BeLessThan(0);
        version2.CompareTo(version1).Should().BeGreaterThan(0);
    }
    
    [Test]
    public void TryParse_WithStarVersion_ShouldReturnTrue()
    {
        var result = SemanticVersion.TryParse("*", out var version);
        Console.WriteLine(version);
        result.Should().BeTrue();
        version.ToString().Should().Be("*");
        version.Wildcard.Should().BeTrue();
    }
    
    [Test]
    public void CompareTo_WithStarMinorVersion_ShouldReturnTrue()
    {
        var result = SemanticVersion.TryParse("2.*", out var version);
        Console.WriteLine(version);
        result.Should().BeTrue();
        version.ToString().Should().Be("2.*");
        version.Major.Should().Be(2);
        version.Minor.Should().Be(-1);
        version.Patch.Should().Be(-1);
        version.Wildcard.Should().BeTrue();
    }
    
    [Test]
    public void CompareTo_WithStarPatchVersion_ShouldReturnTrue()
    {
        var result = SemanticVersion.TryParse("2.1.*", out var version);
        Console.WriteLine(version);
        result.Should().BeTrue();
        version.ToString().Should().Be("2.1.*");
        version.Major.Should().Be(2);
        version.Minor.Should().Be(1);
        version.Patch.Should().Be(-1);
        version.Wildcard.Should().BeTrue();
    }
    
    [Test]
    public void CompareTo_WithStarVersion_ShouldCompareMinVersion()
    {
        var version1 = new SemanticVersion(1, 0, 0);
        var version2 = new SemanticVersion(2, 0, 0);
        var version3 = new SemanticVersion(2, 1, 0);
        var version4 = new SemanticVersion(2, 1, 1);
        
        var starVersion = new SemanticVersion(2, -1, -1, wildcard: true);
        
        version1.CompareTo(starVersion).Should().BeLessThan(0);
        version2.CompareTo(starVersion).Should().Be(0);
        version3.CompareTo(starVersion).Should().Be(0);
        version4.CompareTo(starVersion).Should().Be(0);
    }
}