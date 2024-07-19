using System;

namespace Mal.DocumentGenerator.DocExtensions;

public record Summary
{
    public required string Target { get; init; }
    public required string AuthorNickname { get; init; }
    public required string AuthorUserId { get; init; }
    public required DateTimeOffset Date { get; init; }
    public bool ReplacesOriginal { get; init; } = false;
    public required string SummaryText { get; init; }
}