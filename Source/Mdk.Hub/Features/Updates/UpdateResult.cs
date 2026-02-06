using System;
using System.Collections.Generic;

namespace Mdk.Hub.Features.Updates;

/// <summary>
///     Result of an update operation.
/// </summary>
public record UpdateResult
{
    /// <summary>
    ///     Gets whether the update operation succeeded.
    /// </summary>
    public required bool Success { get; init; }
    
    /// <summary>
    ///     Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    ///     Gets the exception that caused the failure, if any.
    /// </summary>
    public Exception? Exception { get; init; }
    
    /// <summary>
    ///     Gets the list of items that were successfully updated.
    /// </summary>
    public IReadOnlyList<string> UpdatedItems { get; init; } = Array.Empty<string>();
}