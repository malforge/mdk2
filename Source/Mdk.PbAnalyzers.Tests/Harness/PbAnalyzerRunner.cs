using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Mdk2.PbAnalyzers.Tests.Harness;

/// <summary>
///     Runs <see cref="ScriptAnalyzer" /> over in-memory sources compiled against <see cref="StubGameAssemblies" />.
/// </summary>
static class PbAnalyzerRunner
{
    /// <summary>
    ///     Programmable block projects target C# 6, so the tests compile at that level too.
    /// </summary>
    const LanguageVersion ScriptLanguageVersion = LanguageVersion.CSharp6;

    /// <summary>
    ///     A platform-neutral stand-in for the project directory. Never touched on disk; it only has to be a consistent
    ///     prefix of the source file paths so that the analyzer's ignore matching has something to work relative to.
    /// </summary>
    public static readonly string ProjectDir = Path.Combine(Path.GetTempPath(), "MdkPbAnalyzerTests");

    public static AnalysisResult Run(string source, string? ini = null, string fileName = "Program.cs")
        => Run([new SourceFile(fileName, source)], ini);

    public static AnalysisResult Run(IReadOnlyList<SourceFile> sources, string? ini = null)
    {
        var trees = sources
            .Select(s => CSharpSyntaxTree.ParseText(
                SourceText.From(s.Content),
                new CSharpParseOptions(ScriptLanguageVersion),
                Path.Combine(ProjectDir, s.Name)))
            .ToImmutableArray();

        var compilation = CSharpCompilation.Create(
            "Script",
            trees,
            StubGameAssemblies.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var additionalFiles = ImmutableArray<AdditionalText>.Empty;
        if (ini != null)
            additionalFiles = [new TestAdditionalText(Path.Combine(ProjectDir, "mdk.ini"), ini)];

        var options = new AnalyzerOptions(additionalFiles, new TestAnalyzerConfigOptionsProvider(ProjectDir));

        // A fresh analyzer instance per run: the analyzer keeps per-compilation state in instance fields, so sharing one
        // between runs would let settings from one test leak into the next.
        var withAnalyzers = compilation.WithAnalyzers([new ScriptAnalyzer()], options);
        var analyzerDiagnostics = withAnalyzers.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();

        var crash = analyzerDiagnostics.FirstOrDefault(d => d.Id == "AD0001");
        if (crash != null)
            throw new InvalidOperationException($"The analyzer threw an exception: {crash.GetMessage()}");

        return new AnalysisResult(analyzerDiagnostics, compilation.GetDiagnostics());
    }

    sealed class TestAdditionalText(string path, string content) : AdditionalText
    {
        public override string Path { get; } = path;
        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(content);
    }

    sealed class TestAnalyzerConfigOptionsProvider(string projectDir) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new TestAnalyzerConfigOptions(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["build_property.projectdir"] = projectDir
            });

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => TestAnalyzerConfigOptions.Empty;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => TestAnalyzerConfigOptions.Empty;
    }

    sealed class TestAnalyzerConfigOptions(Dictionary<string, string> values) : AnalyzerConfigOptions
    {
        public static readonly AnalyzerConfigOptions Empty = new TestAnalyzerConfigOptions(new Dictionary<string, string>());

        public override IEnumerable<string> Keys => values.Keys;

        public override bool TryGetValue(string key, out string value) => values.TryGetValue(key, out value!);
    }
}

record SourceFile(string Name, string Content);

/// <summary>
///     The analyzer's own diagnostics, plus the compiler's, so that tests can prove a scenario actually compiled rather
///     than passing because a namespace failed to bind.
/// </summary>
record AnalysisResult(ImmutableArray<Diagnostic> Analyzer, ImmutableArray<Diagnostic> Compiler)
{
    public IEnumerable<Diagnostic> OfRule(string id) => Analyzer.Where(d => d.Id == id);

    public IEnumerable<Diagnostic> CompilerErrors => Compiler.Where(d => d.Severity == DiagnosticSeverity.Error);

    public string Describe()
    {
        var lines = Analyzer.Select(d => $"  [{d.Id}] {d.GetMessage()} @ {Where(d)}")
            .Concat(CompilerErrors.Select(d => $"  [compiler {d.Id}] {d.GetMessage()} @ {Where(d)}"))
            .ToArray();
        return lines.Length == 0 ? "  (none)" : string.Join(Environment.NewLine, lines);
    }

    static string Where(Diagnostic diagnostic)
    {
        var span = diagnostic.Location.GetLineSpan();
        return $"line {span.StartLinePosition.Line + 1} '{SourceTextOf(diagnostic)}'";
    }

    static string SourceTextOf(Diagnostic diagnostic)
    {
        var tree = diagnostic.Location.SourceTree;
        return tree == null ? "?" : tree.GetText().ToString(diagnostic.Location.SourceSpan);
    }
}
