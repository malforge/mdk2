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

/// <summary>
///     Wraps the result of an API or service call, forcing callers to handle both success and failure cases.
///     Use <see cref="TryGetValue" /> to access the result value.
/// </summary>
/// <typeparam name="T">The type of the result value.</typeparam>
public readonly struct ApiResult<T>
{
    readonly T _value;
    readonly bool _hasValue;

    ApiResult(T value)
    {
        _value = value;
        _hasValue = true;
        IsSuccess = true;
        ErrorMessage = null;
        ErrorKind = default;
    }

    ApiResult(string errorMessage, ApiErrorKind kind)
    {
        _value = default!;
        _hasValue = false;
        IsSuccess = false;
        ErrorMessage = errorMessage;
        ErrorKind = kind;
    }

    /// <summary>Whether the call succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>The human-readable error message if the call failed; otherwise <c>null</c>.</summary>
    public string? ErrorMessage { get; }

    /// <summary>The error category if the call failed.</summary>
    public ApiErrorKind ErrorKind { get; }

    /// <summary>
    ///     Attempts to get the result value.
    /// </summary>
    /// <param name="value">The result value if the call succeeded.</param>
    /// <returns><c>true</c> if the call succeeded and <paramref name="value" /> is valid; otherwise <c>false</c>.</returns>
    public bool TryGetValue(out T value)
    {
        value = _value;
        return _hasValue;
    }

    /// <summary>Creates a successful result wrapping <paramref name="value" />.</summary>
    public static ApiResult<T> Ok(T value) => new(value);

    /// <summary>Creates a failed result with the given error message and optional error kind.</summary>
    public static ApiResult<T> Fail(string message, ApiErrorKind kind = ApiErrorKind.Unknown) => new(message, kind);
}
