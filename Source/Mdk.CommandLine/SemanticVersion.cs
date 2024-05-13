using System;
using System.Reflection;

namespace Mdk.CommandLine;

/// <summary>
///     An implementation of the Semantic Versioning specification.
/// </summary>
/// <param name="major"></param>
/// <param name="minor"></param>
/// <param name="patch"></param>
/// <param name="preRelease"></param>
/// <param name="buildMetadata"></param>
public readonly struct SemanticVersion(int major, int minor, int patch, string? preRelease = null, string? buildMetadata = null, bool wildcard = false)
    : IComparable<SemanticVersion>, IFormattable, IEquatable<SemanticVersion>
{
    /// <summary>
    ///     An empty semantic version.
    /// </summary>
    public static readonly SemanticVersion Empty = new();

    /// <summary>
    ///     Determines if one semantic version is lower than another.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator <(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) < 0;

    /// <summary>
    ///     Determines if one semantic version is greater than another.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator >(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) > 0;

    /// <summary>
    ///     Determines if one semantic version is lower than or equal to another.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator <=(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) <= 0;

    /// <summary>
    ///     Determines if one semantic version is greater than or equal to another.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator >=(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) >= 0;

    /// <summary>
    ///     Determines if two semantic versions are equal.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) == 0;

    /// <summary>
    ///     Determines if two semantic versions are not equal.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) != 0;

    /// <summary>
    /// Whether the last element is a *.
    /// </summary>
    public bool Wildcard { get; } = wildcard;
    
    /// <summary>
    ///     The major version number: "1" in "1.2.3".
    /// </summary>
    public int Major { get; } = major;

    /// <summary>
    ///     The minor version number: "2" in "1.2.3".
    /// </summary>
    public int Minor { get; } = minor;

    /// <summary>
    ///     The patch version number: "3" in "1.2.3".
    /// </summary>
    public int Patch { get; } = patch;

    /// <summary>
    ///     The pre-release version: "alpha" in "1.2.3-alpha".
    /// </summary>
    public string? PreRelease { get; } = preRelease;

    /// <summary>
    ///     The build metadata: "build123" in "1.2.3+build123". Note the + as opposed to - for pre-release.
    /// </summary>
    public string? BuildMetadata { get; } = buildMetadata;

    /// <summary>
    ///     Determines if the semantic version is empty.
    /// </summary>
    /// <returns></returns>
    public bool IsEmpty() => Major == 0 && Minor == 0 && Patch == 0 && PreRelease == null && BuildMetadata == null;

    /// <summary>
    ///     Determines if the semantic version is a pre-release version.
    /// </summary>
    /// <returns></returns>
    public bool IsPrerelease() => PreRelease != null;

    /// <summary>
    ///     Determines if two semantic versions are equal.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(SemanticVersion other) => CompareTo(other) == 0;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is SemanticVersion other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, PreRelease, BuildMetadata);

    /// <summary>
    ///     Creates a string representation of the semantic version.
    /// </summary>
    /// <returns></returns>
    public override string ToString() => ToString("V", null);

    /// <summary>
    ///     Creates a string representation of the semantic version.
    /// </summary>
    /// <param name="format"></param>
    /// <param name="formatProvider"></param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (string.IsNullOrEmpty(format) || format == "V")
        {
            if (Wildcard)
            {
                if (Major < 0)
                    return "*";
                if (Minor < 0)
                    return $"{Major}.*";
                if (Patch < 0)
                    return $"{Major}.{Minor}.*";
            }
            var version = $"{Major}.{Minor}.{Patch}";
            if (PreRelease != null)
                version += $"-{PreRelease}";
            if (BuildMetadata != null)
                version += $"+{BuildMetadata}";
            return version;
        }
        if (format == "S" || PreRelease == null)
            return $"{Major}.{Minor}.{Patch}";
        if (format == "P")
            return $"{Major}.{Minor}.{Patch}-{PreRelease}";
        throw new FormatException($"The format string '{format}' is not supported.");
    }

    /// <summary>
    ///     Extracts a semantic version from an assembly's informational version attribute.
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static SemanticVersion FromAssemblyInformationalVersion(Assembly assembly)
    {
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (version == null)
            throw new ArgumentException("The assembly does not have an informational version.");
        if (!TryParse(version, out var semanticVersion))
            throw new ArgumentException("The assembly informational version is not a valid semantic version.");
        return semanticVersion;
    }

    /// <summary>
    ///     Extracts a semantic version from an assembly's version attribute.
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static SemanticVersion FromAssemblyVersion(Assembly assembly)
    {
        var version = assembly.GetName().Version;
        if (version == null)
            throw new ArgumentException("The assembly does not have a version.");
        return new SemanticVersion(version.Major, version.Minor, version.Build);
    }

    /// <summary>
    ///     Attempts to parse a semantic version from a string.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    public static bool TryParse(string input, out SemanticVersion version)
    {
        version = default;
        var parts = input.Split('-', '+');
        var versionParts = parts[0].Split('.');
        if (versionParts[0] == "*" && versionParts.Length == 1)
        {
            version = new SemanticVersion(-1, -1, -1, null, null, true);
            return true;
        }
        if (versionParts.Length < 2)
            return false;
        if (!int.TryParse(versionParts[0], out var major))
            return false;
        if (versionParts[1] == "*" && versionParts.Length == 2)
        {
            version = new SemanticVersion(major, -1, -1, null, null, true);
            return true;
        }
        if (versionParts.Length < 3)
            return false;
        if (!int.TryParse(versionParts[1], out var minor))
            return false;
        if (versionParts[2] == "*" && versionParts.Length == 3)
        {
            version = new SemanticVersion(major, minor, -1, null, null, true);
            return true;
        }
        if (!int.TryParse(versionParts[2], out var patch))
            return false;
        string? preRelease = null;
        if (parts.Length > 1)
            preRelease = parts[1];
        string? buildMetadata = null;
        if (parts.Length > 2)
            buildMetadata = parts[2];
        version = new SemanticVersion(major, minor, patch, preRelease, buildMetadata);
        return true;
    }

    /// <inheritdoc />
    public int CompareTo(SemanticVersion other)
    {
        if (Wildcard && !other.Wildcard)
        {
            var major = Major >= 0? Major : other.Major;
            var minor = Minor >= 0? Minor : other.Minor;
            var patch = Patch >= 0? Patch : other.Patch;
            
            return new SemanticVersion(major, minor, patch).CompareTo(other);
        }
        if (!Wildcard && other.Wildcard)
        {
            return -other.CompareTo(this);
        }
        
        var self = Wildcard? new SemanticVersion(Math.Max(0, Major), Math.Max(0, Minor), Math.Max(0, Patch)) : this;
        if (other.Wildcard)
            other = new SemanticVersion(Math.Max(0, other.Major), Math.Max(0, other.Minor), Math.Max(0, other.Patch));
        
        var majorComparison = self.Major.CompareTo(other.Major);
        if (majorComparison != 0)
            return majorComparison;
        var minorComparison = self.Minor.CompareTo(other.Minor);
        if (minorComparison != 0)
            return minorComparison;
        var patchComparison = self.Patch.CompareTo(other.Patch);
        if (patchComparison != 0)
            return patchComparison;
        if (self.PreRelease is null && other.PreRelease is null)
            return 0;
        if (other.PreRelease is null)
            return -1;
        if (self.PreRelease is null)
            return 1;

        var preRelease = self.PreRelease;
        var otherPreRelease = other.PreRelease;

        if (char.IsDigit(preRelease[^1]) && char.IsDigit(otherPreRelease[^1]))
        {
            var preReleaseNumber = 0;
            var otherPreReleaseNumber = 0;
            var preReleaseNumberStart = preRelease.Length - 1;
            var otherPreReleaseNumberStart = otherPreRelease.Length - 1;
            while (preReleaseNumberStart >= 0 && char.IsDigit(preRelease[preReleaseNumberStart]))
                preReleaseNumberStart--;
            while (otherPreReleaseNumberStart >= 0 && char.IsDigit(otherPreRelease[otherPreReleaseNumberStart]))
                otherPreReleaseNumberStart--;
            if (preReleaseNumberStart < preRelease.Length - 1)
                preReleaseNumber = int.Parse(preRelease[(preReleaseNumberStart + 1)..]);
            if (otherPreReleaseNumberStart < otherPreRelease.Length - 1)
                otherPreReleaseNumber = int.Parse(otherPreRelease[(otherPreReleaseNumberStart + 1)..]);
            var preReleaseComparison = string.Compare(preRelease[..(preReleaseNumberStart + 1)], otherPreRelease[..(otherPreReleaseNumberStart + 1)], StringComparison.Ordinal);
            if (preReleaseComparison != 0)
                return preReleaseComparison;
            return preReleaseNumber.CompareTo(otherPreReleaseNumber);
        }
        else
        {
            var preReleaseComparison = string.Compare(self.PreRelease, other.PreRelease, StringComparison.Ordinal);
            if (preReleaseComparison != 0)
                return preReleaseComparison;
        }
        return string.Compare(self.BuildMetadata, other.BuildMetadata, StringComparison.Ordinal);
    }
}