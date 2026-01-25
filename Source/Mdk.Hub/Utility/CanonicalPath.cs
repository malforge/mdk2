using System;
using System.IO;
using System.Text.Json.Serialization;

namespace Mdk.Hub.Utility;

/// <summary>
///     Represents a normalized, platform-aware file system path suitable for reliable comparisons.
/// </summary>
/// <remarks>
///     <para>
///         CanonicalPath resolves relative paths to absolute, normalizes directory separators,
///         and applies platform-specific case sensitivity rules to ensure consistent path comparisons.
///     </para>
///     <para>
///         On Windows, paths are compared case-insensitively but preserve their original casing.
///         On Unix-like systems, paths are compared case-sensitively.
///     </para>
/// </remarks>
[JsonConverter(typeof(CanonicalPathJsonConverter))]
public readonly struct CanonicalPath : IEquatable<CanonicalPath>
{
    // /// <summary>
    // ///    Implicitly converts a <see cref="CanonicalPath" /> to its underlying string representation.
    // /// </summary>
    // /// <param name="path"></param>
    // /// <returns></returns>
    // public static implicit operator string?(CanonicalPath path) => path.Value;
    
    /// <summary>
    ///     Determines whether the current path is equal to another canonical path.
    /// </summary>
    /// <param name="other">The path to compare with this instance.</param>
    /// <returns><see langword="true" /> if the paths are equal; otherwise, <see langword="false" />.</returns>
    public bool Equals(CanonicalPath other)
    {
        var comparison = OperatingSystem.IsWindows() 
            ? StringComparison.OrdinalIgnoreCase 
            : StringComparison.Ordinal;
        return string.Equals(Value, other.Value, comparison);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is CanonicalPath other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (Value == null)
            return 0;
            
        return OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase.GetHashCode(Value)
            : Value.GetHashCode();
    }

    /// <summary>
    ///     Determines whether the canonical path is empty (i.e., has no value).
    /// </summary>
    /// <returns></returns>
    public bool IsEmpty() => Value is null;

    /// <summary>
    ///     Determines whether two canonical paths are equal.
    /// </summary>
    public static bool operator ==(CanonicalPath left, CanonicalPath right) => left.Equals(right);

    /// <summary>
    ///     Determines whether two canonical paths are not equal.
    /// </summary>
    public static bool operator !=(CanonicalPath left, CanonicalPath right) => !left.Equals(right);

    /// <summary>
    ///     Initializes a new instance of <see cref="CanonicalPath" /> from the specified path.
    /// </summary>
    /// <param name="path">The file system path to canonicalize. Cannot be <see langword="null" />.</param>
    /// <param name="baseDirectory">
    ///     Optional base directory for resolving relative paths.
    ///     If <see langword="null" /> or empty, relative paths are resolved against the current working directory.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path" /> is <see langword="null" />.</exception>
    /// <remarks>
    ///     <para>
    ///         The constructor performs the following normalizations:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Trims leading and trailing whitespace from the input path</description>
    ///         </item>
    ///         <item>
    ///             <description>Resolves relative paths to absolute using the specified base directory or current directory</description>
    ///         </item>
    ///         <item>
    ///             <description>Removes trailing directory separators (except for root paths like "C:\" or "/")</description>
    ///         </item>
    ///         <item>
    ///             <description>Normalizes directory separators to the platform's preferred separator</description>
    ///         </item>
    ///         <item>
    ///             <description>On Windows: Converts the path to lowercase for case-insensitive comparisons</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public CanonicalPath(string path, string? baseDirectory = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        path = path.Trim();

        // Make relative paths deterministic by anchoring them to a known base.
        // If baseDirectory is omitted, it uses current directory (GetFullPath does that).
        string fullPath;
        if (!string.IsNullOrEmpty(baseDirectory) && !Path.IsPathRooted(path))
            fullPath = Path.GetFullPath(Path.Combine(baseDirectory, path));
        else
            fullPath = Path.GetFullPath(path);

        // Remove trailing separators, but never trim the root itself.
        var root = Path.GetPathRoot(fullPath) ?? string.Empty;
        if (fullPath.Length > root.Length)
            fullPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (Path.AltDirectorySeparatorChar != Path.DirectorySeparatorChar)
            fullPath = fullPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        Value = fullPath;
    }

    /// <summary>
    ///     Gets the normalized, absolute path string.
    /// </summary>
    /// <remarks>
    ///     This value is guaranteed to be:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>An absolute path</description>
    ///         </item>
    ///         <item>
    ///             <description>Free of trailing directory separators (except root paths)</description>
    ///         </item>
    ///         <item>
    ///             <description>Using the platform's preferred directory separator</description>
    ///         </item>
    ///         <item>
    ///             <description>Lowercase on Windows for case-insensitive comparisons</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public readonly string? Value;

    /// <summary>
    ///    Returns the file name of the canonical path.
    /// </summary>
    /// <returns></returns>
    public string GetFileName()
    {
        if (Value is null)
            return string.Empty;
        return Path.GetFileName(Value);
    }
    
    /// <summary>
    ///    Returns the directory name of the canonical path.
    /// </summary>
    /// <returns></returns>
    public string GetDirectoryName()
    {
        if (Value is null)
            return string.Empty;
        return Path.GetDirectoryName(Value) ?? string.Empty;
    }
    
    /// <summary>
    ///     Returns the canonical path as a string.
    /// </summary>
    /// <returns>The normalized absolute path.</returns>
    public override string ToString() => Value ?? string.Empty;
}