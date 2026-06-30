using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.Shared.AttributeTrimming;

public sealed record AttributeTrimmingResult(
    Project Project,
    ImmutableArray<AttributeTrimmingDiagnostic> Diagnostics,
    AttributeTrimmingReport Report);
