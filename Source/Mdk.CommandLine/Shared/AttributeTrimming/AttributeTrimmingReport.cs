using System.Collections.Immutable;

namespace Mdk.CommandLine.Shared.AttributeTrimming;

public sealed record AttributeTrimmingReport(
    ImmutableArray<AttributeTrimmingReportEntry> TrimmedApplications,
    ImmutableArray<AttributeTrimmingReportEntry> TrimmedDeclarations,
    ImmutableArray<AttributeTrimmingReportEntry> PreservedApplications);

public sealed record AttributeTrimmingReportEntry(
    string Symbol,
    string? File,
    int Line,
    string Reason);
