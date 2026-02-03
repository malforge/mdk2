using System.Collections.Generic;

namespace Mdk.Hub.Utility;

/// <summary>
///     Provides equality comparison for <see cref="CanonicalPath" /> instances.
/// </summary>
/// <remarks>
///     This comparer is optimized for use with <see cref="Dictionary{TKey,TValue}" />
///     and <see cref="HashSet{T}" /> to ensure efficient path-based lookups.
/// </remarks>
public sealed class CanonicalPathComparer : IEqualityComparer<CanonicalPath>
{
    private CanonicalPathComparer() { }

    /// <summary>
    ///     Gets the singleton instance of <see cref="CanonicalPathComparer" />.
    /// </summary>
    public static CanonicalPathComparer Instance { get; } = new();

    /// <inheritdoc />
    public bool Equals(CanonicalPath x, CanonicalPath y) => x.Value == y.Value;

    /// <inheritdoc />
    public int GetHashCode(CanonicalPath obj) => obj.Value?.GetHashCode() ?? 0;
}
