using System;
using System.Reflection;

namespace Mdk.CommandLine;

public readonly struct SemanticVersion(int major, int minor, int patch, string? preRelease = null, string? buildMetadata = null)
    : IComparable<SemanticVersion>, IFormattable
{
    public int Major { get; } = major;
    public int Minor { get; } = minor;
    public int Patch { get; } = patch;
    public string? PreRelease { get; } = preRelease;
    public string? BuildMetadata { get; } = buildMetadata;

    public override string ToString() => ToString("V", null);

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (string.IsNullOrEmpty(format) || format == "V")
        {
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

    public static SemanticVersion FromAssemblyInformationalVersion(Assembly assembly)
    {
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (version == null)
            throw new ArgumentException("The assembly does not have an informational version.");
        if (!TryParse(version, out var semanticVersion))
            throw new ArgumentException("The assembly informational version is not a valid semantic version.");
        return semanticVersion;
    }
    
    public static SemanticVersion FromAssemblyVersion(Assembly assembly)
    {
        var version = assembly.GetName().Version;
        if (version == null)
            throw new ArgumentException("The assembly does not have a version.");
        return new SemanticVersion(version.Major, version.Minor, version.Build, null, null);
    }

    public static bool TryParse(string input, out SemanticVersion version)
    {
        version = default;
        var parts = input.Split('-', '+');
        var versionParts = parts[0].Split('.');
        if (versionParts.Length != 3)
            return false;
        if (!int.TryParse(versionParts[0], out var major))
            return false;
        if (!int.TryParse(versionParts[1], out var minor))
            return false;
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

    public int CompareTo(SemanticVersion other)
    {
        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0)
            return majorComparison;
        var minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0)
            return minorComparison;
        var patchComparison = Patch.CompareTo(other.Patch);
        if (patchComparison != 0)
            return patchComparison;
        var preRelease = PreRelease ?? string.Empty;
        var otherPreRelease = other.PreRelease ?? string.Empty;
        
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
            var preReleaseComparison = string.Compare(PreRelease, other.PreRelease, StringComparison.Ordinal);
            if (preReleaseComparison != 0)
                return preReleaseComparison;
        }
        return string.Compare(BuildMetadata, other.BuildMetadata, StringComparison.Ordinal);
    }
}