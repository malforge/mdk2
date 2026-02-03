using System;
using System.Collections.Generic;

namespace Mdk.Hub.Features.Updates;

/// <summary>
///     Result of an update operation.
/// </summary>
public record UpdateResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Exception? Exception { get; init; }
    public IReadOnlyList<string> UpdatedItems { get; init; } = Array.Empty<string>();
}

/// <summary>
///     Progress information for update operations.
/// </summary>
public record UpdateProgress
{
    public required string Message { get; init; }
    public double? PercentComplete { get; init; }
}

