namespace Mdk.Hub.Framework;

/// <summary>
///     Error categories for <see cref="ApiResult{T}" />.
/// </summary>
public enum ApiErrorKind
{
    /// <summary>Unknown or unclassified error.</summary>
    Unknown,

    /// <summary>The data source was not found or is unavailable (e.g. SE not installed).</summary>
    Unavailable,

    /// <summary>The requested item was not found.</summary>
    NotFound,

    /// <summary>A parsing or format error occurred.</summary>
    ParseError
}