using System.Collections.Immutable;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Mdk.CommandLine.Shared.AttributeTrimming;

public sealed record AttributeTrimmingPlan(
    ImmutableDictionary<DocumentId, ImmutableHashSet<TextSpan>> AttributeApplications,
    ImmutableDictionary<DocumentId, ImmutableHashSet<TextSpan>> AttributeDeclarations,
    ImmutableHashSet<INamedTypeSymbol> SourceDefinedAttributeTypes,
    ImmutableArray<AttributeTrimmingDiagnostic> Diagnostics,
    AttributeTrimmingReport Report)
{
    public static readonly IEqualityComparer<INamedTypeSymbol> SymbolComparer = SymbolEqualityComparer.Default;
}
