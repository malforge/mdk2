using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace Mdk2.PbAnalyzers.Tests;

/// <summary>
///     Measures what the analyzer costs on a large real project, so the cost of a rule can be argued from numbers.
/// </summary>
/// <remarks>
///     Explicit, environment driven, and deliberately loads the analyzer from a path rather than using the referenced
///     project, so that two builds of it can be compared by running this once against each.
///     <code>
///     $env:MDK_BENCH_ANALYZER = '...\bin\Release\netstandard2.0\Mal.Mdk2.PbAnalyzers.dll'
///     $env:MDK_BENCH_PROJECT  = '...\TestData\AutomaticLCDs2MDK2'
///     $env:MDK_SE_BIN         = '...\SpaceEngineers\Bin64'
///     $env:MDK_BENCH_RUNS     = '10'
///     </code>
/// </remarks>
[TestFixture, Explicit]
public class AnalyzerBenchmark
{
    const string NetFrameworkReferenceAssemblies =
        @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8";

    /// <summary>
    ///     Compares several builds of the analyzer in one process, interleaving their samples so that drift in machine
    ///     state affects each of them equally instead of whichever ran last.
    /// </summary>
    [Test]
    public void Compare()
    {
        var spec = Environment.GetEnvironmentVariable("MDK_BENCH_ANALYZERS") ?? "";
        var projectPath = Environment.GetEnvironmentVariable("MDK_BENCH_PROJECT") ?? "";
        var seBin = Environment.GetEnvironmentVariable("MDK_SE_BIN") ?? "";
        var runs = int.TryParse(Environment.GetEnvironmentVariable("MDK_BENCH_RUNS"), out var n) ? n : 25;

        if (spec.Length == 0 || !Directory.Exists(projectPath) || !Directory.Exists(seBin))
            Assert.Inconclusive("Set MDK_BENCH_ANALYZERS as label=path;label=path, plus MDK_BENCH_PROJECT and MDK_SE_BIN.");

        var variants = spec.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(entry => entry.Split('=', 2))
            .Select(parts => (Label: parts[0], Path: parts[1]))
            .ToList();

        foreach (var variant in variants)
            Assert.That(File.Exists(variant.Path), Is.True, $"missing analyzer: {variant.Path}");

        var references = BuildReferences(seBin);
        var trees = LoadTrees(projectPath);
        var options = BuildOptions(projectPath);

        TestContext.Out.WriteLine($"project: {projectPath}  files: {trees.Length}  references: {references.Length}");
        TestContext.Out.WriteLine($"cores: {Environment.ProcessorCount}  runs: {runs} interleaved (plus 3 warmup each)");
        foreach (var variant in variants)
            TestContext.Out.WriteLine($"  {variant.Label,-24} {FileVersionInfo.GetVersionInfo(variant.Path).FileVersion}  {variant.Path}");

        var loaded = variants.ToDictionary(v => v.Label, v => LoadAnalyzers(v.Path));
        var concurrent = variants.ToDictionary(v => v.Label, _ => new List<double>());
        var serial = variants.ToDictionary(v => v.Label, _ => new List<double>());
        var control = new List<double>();

        double RunOnce(ImmutableArray<DiagnosticAnalyzer> analyzers, bool concurrentAnalysis)
        {
            var compilation = NewCompilation(trees, references);
            var withAnalyzers = compilation.WithAnalyzers(analyzers,
                new CompilationWithAnalyzersOptions(options, null, concurrentAnalysis, false));
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var stopwatch = Stopwatch.StartNew();
            withAnalyzers.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
            stopwatch.Stop();
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        // A faster analyzer that stopped reporting things would look like a win and be a disaster, so record what each
        // variant actually finds before timing anything.
        TestContext.Out.WriteLine("");
        foreach (var variant in variants)
        {
            var compilation = NewCompilation(trees, references);
            var diagnostics = compilation.WithAnalyzers(loaded[variant.Label], options)
                .GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
            var byId = diagnostics.GroupBy(d => d.Id).OrderBy(g => g.Key)
                .Select(g => $"{g.Key}={g.Count()}");
            TestContext.Out.WriteLine($"FINDINGS {variant.Label,-24} total={diagnostics.Length,-5} {string.Join(" ", byId)}");
        }

        foreach (var variant in variants)
            for (var i = 0; i < 3; i++)
            {
                RunOnce(loaded[variant.Label], true);
                RunOnce(loaded[variant.Label], false);
            }

        for (var i = 0; i < runs; i++)
        {
            foreach (var variant in variants)
            {
                concurrent[variant.Label].Add(RunOnce(loaded[variant.Label], true));
                serial[variant.Label].Add(RunOnce(loaded[variant.Label], false));
            }

            var compilation = NewCompilation(trees, references);
            GC.Collect();
            var stopwatch = Stopwatch.StartNew();
            compilation.GetDiagnostics();
            stopwatch.Stop();
            control.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        TestContext.Out.WriteLine("");
        Report("compile only (control, no analyzers)", control);
        TestContext.Out.WriteLine("-- concurrent (as a build actually runs) --");
        foreach (var variant in variants)
            Report(variant.Label, concurrent[variant.Label]);
        TestContext.Out.WriteLine("-- serial (proxy for a low core machine) --");
        foreach (var variant in variants)
            Report(variant.Label, serial[variant.Label]);

        var first = variants[0].Label;
        TestContext.Out.WriteLine("");
        foreach (var variant in variants.Skip(1))
        {
            var dc = Median(concurrent[variant.Label]) - Median(concurrent[first]);
            var ds = Median(serial[variant.Label]) - Median(serial[first]);
            TestContext.Out.WriteLine(
                $"DELTA vs {first}: {variant.Label,-24} concurrent {dc,+7:F1} ms ({dc / Median(concurrent[first]) * 100,+5:F1}%)   serial {ds,+7:F1} ms ({ds / Median(serial[first]) * 100,+5:F1}%)");
        }
    }

    [Test]
    public void Benchmark()
    {
        var analyzerPath = Environment.GetEnvironmentVariable("MDK_BENCH_ANALYZER") ?? "";
        var projectPath = Environment.GetEnvironmentVariable("MDK_BENCH_PROJECT") ?? "";
        var seBin = Environment.GetEnvironmentVariable("MDK_SE_BIN") ?? "";
        var runs = int.TryParse(Environment.GetEnvironmentVariable("MDK_BENCH_RUNS"), out var n) ? n : 10;

        if (!File.Exists(analyzerPath) || !Directory.Exists(projectPath) || !Directory.Exists(seBin))
            Assert.Inconclusive("Set MDK_BENCH_ANALYZER, MDK_BENCH_PROJECT and MDK_SE_BIN.");

        var references = BuildReferences(seBin);
        var trees = LoadTrees(projectPath);
        var analyzers = LoadAnalyzers(analyzerPath);
        var options = BuildOptions(projectPath);

        TestContext.Out.WriteLine($"analyzer : {analyzerPath}");
        TestContext.Out.WriteLine($"version  : {FileVersionInfo.GetVersionInfo(analyzerPath).FileVersion}");
        TestContext.Out.WriteLine($"project  : {projectPath}");
        TestContext.Out.WriteLine($"files    : {trees.Length}, references: {references.Length}, analyzers: {analyzers.Length}");
        TestContext.Out.WriteLine($"runs     : {runs} (plus 3 warmup)");

        // Binding only, no analyzers: the floor a build pays regardless, and the thing analyzer cost sits on top of.
        var compileOnly = Measure(runs, () =>
        {
            var compilation = NewCompilation(trees, references);
            return compilation.GetDiagnostics().Length;
        });

        var withAnalyzers = Measure(runs, () =>
        {
            var compilation = NewCompilation(trees, references);
            var withAnalyzersCompilation = compilation.WithAnalyzers(analyzers, options);
            return withAnalyzersCompilation.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult().Length;
        });

        // Concurrency off: a stand-in for a machine with few cores, where the analyzer's total CPU cost cannot be hidden
        // behind parallelism. The measuring machine has plenty of cores, which would otherwise flatter the result.
        var serial = Measure(runs, () =>
        {
            var compilation = NewCompilation(trees, references);
            var withAnalyzersCompilation = compilation.WithAnalyzers(analyzers,
                new CompilationWithAnalyzersOptions(options, null, false, false));
            return withAnalyzersCompilation.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult().Length;
        });

        // Roslyn's own accounting of time spent inside the analyzer, separate from the binding it triggers.
        var telemetry = new List<double>();
        for (var i = 0; i < runs; i++)
        {
            var compilation = NewCompilation(trees, references);
            var withAnalyzersCompilation = compilation.WithAnalyzers(analyzers, options);
            withAnalyzersCompilation.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
            var info = withAnalyzersCompilation.GetAnalyzerTelemetryInfoAsync(analyzers[0], CancellationToken.None)
                .GetAwaiter().GetResult();
            telemetry.Add(info.ExecutionTime.TotalMilliseconds);
        }

        Report("compile only (no analyzers)", compileOnly);
        Report("compile + analyzers (concurrent)", withAnalyzers);
        Report("compile + analyzers (serial, low-core proxy)", serial);
        Report("analyzer execution time (roslyn telemetry)", telemetry);

        TestContext.Out.WriteLine($"RESULT median_compile_only_ms={Median(compileOnly):F1}");
        TestContext.Out.WriteLine($"RESULT median_with_analyzers_ms={Median(withAnalyzers):F1}");
        TestContext.Out.WriteLine($"RESULT median_serial_ms={Median(serial):F1}");
        TestContext.Out.WriteLine($"RESULT median_analyzer_telemetry_ms={Median(telemetry):F1}");
    }

    static void Report(string label, List<double> samples)
    {
        TestContext.Out.WriteLine(
            $"{label,-46} median {Median(samples),8:F1} ms   mean {samples.Average(),8:F1}   min {samples.Min(),8:F1}   max {samples.Max(),8:F1}");
    }

    static double Median(List<double> samples)
    {
        var sorted = samples.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2 : sorted[mid];
    }

    static List<double> Measure(int runs, Func<int> action)
    {
        for (var i = 0; i < 3; i++)
            action();

        var samples = new List<double>();
        for (var i = 0; i < runs; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            samples.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        return samples;
    }

    static CSharpCompilation NewCompilation(ImmutableArray<SyntaxTree> trees, ImmutableArray<MetadataReference> references)
        => CSharpCompilation.Create("Bench", trees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    static ImmutableArray<SyntaxTree> LoadTrees(string projectPath)
        => Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(f => (SyntaxTree)CSharpSyntaxTree.ParseText(
                SourceText.From(File.ReadAllText(f)), new CSharpParseOptions(LanguageVersion.CSharp6), f))
            .ToImmutableArray();

    static ImmutableArray<DiagnosticAnalyzer> LoadAnalyzers(string analyzerPath)
    {
        var reference = new AnalyzerFileReference(analyzerPath, new IsolatedLoader(Path.GetDirectoryName(analyzerPath)!));
        return reference.GetAnalyzers(LanguageNames.CSharp);
    }

    static AnalyzerOptions BuildOptions(string projectPath)
    {
        var additional = ImmutableArray<AdditionalText>.Empty;
        var ini = Directory.GetFiles(projectPath, "*mdk.ini")
            .FirstOrDefault(p => !p.Contains(".local.", StringComparison.OrdinalIgnoreCase));
        if (ini != null)
            additional = [new IniText(ini, File.ReadAllText(ini))];
        return new AnalyzerOptions(additional, new Options(projectPath));
    }

    static ImmutableArray<MetadataReference> BuildReferences(string seBin)
    {
        var builder = ImmutableArray.CreateBuilder<MetadataReference>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dll in Directory.GetFiles(NetFrameworkReferenceAssemblies, "*.dll").Concat(Directory.GetFiles(seBin, "*.dll")))
        {
            if (!seen.Add(Path.GetFileName(dll)))
                continue;
            try { builder.Add(MetadataReference.CreateFromFile(dll)); }
            catch { /* not a managed assembly */ }
        }

        return builder.ToImmutable();
    }

    /// <summary>
    ///     Loads the analyzer under test from its own directory while letting Roslyn itself come from the default
    ///     context, so the two builds of the analyzer can be compared without their identities colliding.
    /// </summary>
    sealed class IsolatedLoader(string directory) : AssemblyLoadContext(true), IAnalyzerAssemblyLoader
    {
        public void AddDependencyLocation(string fullPath) { }

        public Assembly LoadFromPath(string fullPath) => LoadFromAssemblyPath(fullPath);

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name != null && assemblyName.Name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal))
                return null;

            var candidate = Path.Combine(directory, assemblyName.Name + ".dll");
            return File.Exists(candidate) ? LoadFromAssemblyPath(candidate) : null;
        }
    }

    sealed class IniText(string path, string content) : AdditionalText
    {
        public override string Path { get; } = path;
        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(content);
    }

    sealed class Options(string projectDir) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Config(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["build_property.projectdir"] = projectDir,
            ["build_property.mdk-ignorepaths"] = @"obj\**\*;MDK\**\*;**\*.debug.cs"
        });

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Config.Empty;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => Config.Empty;
    }

    sealed class Config(Dictionary<string, string> values) : AnalyzerConfigOptions
    {
        public static readonly AnalyzerConfigOptions Empty = new Config(new Dictionary<string, string>());
        public override IEnumerable<string> Keys => values.Keys;
        public override bool TryGetValue(string key, out string value) => values.TryGetValue(key, out value!);
    }
}
