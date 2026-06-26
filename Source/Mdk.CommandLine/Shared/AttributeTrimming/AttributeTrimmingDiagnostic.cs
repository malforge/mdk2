using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.Shared.AttributeTrimming;

public sealed record AttributeTrimmingDiagnostic(
    string Id,
    string Message,
    Location? Location,
    DiagnosticSeverity Severity = DiagnosticSeverity.Error)
{
    public override string ToString()
    {
        var prefix = Location is { IsInSource: true } location ? $"{location.GetLineSpan().Path}({location.GetLineSpan().StartLinePosition.Line + 1},{location.GetLineSpan().StartLinePosition.Character + 1}): " : string.Empty;
        return $"{prefix}{Id}: {Message}";
    }
}
