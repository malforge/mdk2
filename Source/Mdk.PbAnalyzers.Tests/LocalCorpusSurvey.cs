using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace Mdk2.PbAnalyzers.Tests;

/// <summary>
///     Runs the analyzer across a local collection of real script projects, to see how the rules behave on code nobody
///     wrote for our benefit.
/// </summary>
/// <remarks>
///     Explicit, and driven entirely by environment variables, so it never runs in CI and carries no machine specific
///     paths. Unlike the rest of the suite this compiles against the real game assemblies and the real .NET Framework
///     reference assemblies, which is the only way the results mean anything.
///     <code>
///     $env:MDK_CORPUS_ROOTS = 'D:\Repos\SE\Scripts;E:\Repos\SpaceEngineers'
///     $env:MDK_SE_BIN = 'E:\Steam\steamapps\common\SpaceEngineers\Bin64'
///     dotnet test --filter "FullyQualifiedName~LocalCorpusSurvey" -l "console;verbosity=detailed"
///     </code>
///     Known limitation: every .cs file under a project directory is compiled, whereas older non-SDK projects list
///     their sources explicitly. Such a project can contribute files it does not actually build, packed output kept in
///     an Output folder being the usual case, so read hits against those with that in mind.
/// </remarks>
[TestFixture, Explicit]
public class LocalCorpusSurvey
{
    const string NetFrameworkReferenceAssemblies =
        @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8";

    [Test]
    public void Survey()
    {
        var roots = (Environment.GetEnvironmentVariable("MDK_CORPUS_ROOTS") ?? "")
            .Split(';', StringSplitOptions.RemoveEmptyEntries);
        var seBin = Environment.GetEnvironmentVariable("MDK_SE_BIN") ?? "";

        if (roots.Length == 0 || !Directory.Exists(seBin))
            Assert.Inconclusive("Set MDK_CORPUS_ROOTS and MDK_SE_BIN.");
        if (!Directory.Exists(NetFrameworkReferenceAssemblies))
            Assert.Inconclusive($"Needs the .NET Framework reference assemblies at {NetFrameworkReferenceAssemblies}.");

        var references = BuildReferences(seBin);
        TestContext.Out.WriteLine($"references: {references.Length}");

        var projects = roots.Where(Directory.Exists)
            .SelectMany(root => Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories))
            .Where(p => !IsUnder(p, "obj") && !IsUnder(p, "bin"))
            .Select(p => Path.GetDirectoryName(p)!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(IsProgrammableBlockProject)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        TestContext.Out.WriteLine($"programmable block projects: {projects.Count}");

        var totals = new Dictionary<string, int>();
        var hits = new List<string>();
        var failed = 0;

        foreach (var project in projects)
        {
            ImmutableArray<Diagnostic> diagnostics;
            try
            {
                diagnostics = Analyze(project, references);
            }
            catch (Exception e)
            {
                failed++;
                TestContext.Out.WriteLine($"  !! {Short(project)}: {e.GetType().Name} {e.Message}");
                continue;
            }

            foreach (var d in diagnostics.Where(d => d.Id is "MDK05" or "MDK06" or "AD0001"))
            {
                totals.TryGetValue(d.Id, out var n);
                totals[d.Id] = n + 1;
                var span = d.Location.GetLineSpan();
                hits.Add($"  [{d.Id}] {Short(project)} :: {Path.GetFileName(span.Path)}({span.StartLinePosition.Line + 1}) {d.GetMessage()}");
            }
        }

        TestContext.Out.WriteLine($"projects that failed to analyze: {failed}");
        foreach (var kv in totals.OrderBy(kv => kv.Key))
            TestContext.Out.WriteLine($"TOTAL {kv.Key}: {kv.Value}");

        foreach (var hit in hits)
            TestContext.Out.WriteLine(hit);

        if (hits.Count == 0)
            TestContext.Out.WriteLine("no MDK05/MDK06/AD0001 anywhere in the corpus");
    }

    static string Short(string path) => string.Join('/', path.Split(Path.DirectorySeparatorChar).TakeLast(2));

    static bool IsUnder(string path, string dir) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(s => s.Equals(dir, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    ///     Mod projects and plain libraries must be left out; these rules only describe programmable block scripts.
    /// </summary>
    static bool IsProgrammableBlockProject(string directory)
    {
        var csproj = Directory.GetFiles(directory, "*.csproj").FirstOrDefault();
        if (csproj != null)
        {
            var text = File.ReadAllText(csproj);
            // Must be a package *reference*: MDK's own analyzer sources name the same package and would otherwise be
            // mistaken for a script project.
            if (text.Contains("PackageReference Include=\"Mal.Mdk2.ModPackager", StringComparison.OrdinalIgnoreCase)
                || text.Contains("PackageReference Include=\"Mal.Mdk2.ModAnalyzers", StringComparison.OrdinalIgnoreCase))
                return false;
            if (text.Contains("PackageReference Include=\"Mal.Mdk2.PbPackager", StringComparison.OrdinalIgnoreCase)
                || text.Contains("PackageReference Include=\"Mal.Mdk2.PbAnalyzers", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        foreach (var ini in Directory.GetFiles(directory, "*mdk.ini"))
        {
            if (File.ReadAllText(ini).Contains("type=programmableblock", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // MDK1 projects keep their configuration in an MDK folder next to the project.
        return Directory.Exists(Path.Combine(directory, "MDK"));
    }

    static ImmutableArray<Diagnostic> Analyze(string directory, ImmutableArray<MetadataReference> references)
    {
        var files = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(f => !IsUnder(f, "obj") && !IsUnder(f, "bin"))
            .ToList();
        if (files.Count == 0)
            return ImmutableArray<Diagnostic>.Empty;

        var trees = files.Select(f => CSharpSyntaxTree.ParseText(
                SourceText.From(File.ReadAllText(f)),
                new CSharpParseOptions(LanguageVersion.CSharp6),
                f))
            .ToImmutableArray();

        var compilation = CSharpCompilation.Create("Corpus", trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Feed the project's own ini in, so the analyzer honours the ignores and namespaces it really has.
        var additional = ImmutableArray<AdditionalText>.Empty;
        var iniPath = Directory.GetFiles(directory, "*mdk.ini").FirstOrDefault(p => !p.Contains(".local.", StringComparison.OrdinalIgnoreCase));
        if (iniPath != null)
            additional = [new IniText(iniPath, File.ReadAllText(iniPath))];

        var options = new AnalyzerOptions(additional, new Options(directory));
        return compilation.WithAnalyzers([new ScriptAnalyzer()], options)
            .GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
    }

    static ImmutableArray<MetadataReference> BuildReferences(string seBin)
    {
        var builder = ImmutableArray.CreateBuilder<MetadataReference>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dll in Directory.GetFiles(NetFrameworkReferenceAssemblies, "*.dll")
                     .Concat(Directory.GetFiles(seBin, "*.dll")))
        {
            if (!seen.Add(Path.GetFileName(dll)))
                continue;
            try
            {
                builder.Add(MetadataReference.CreateFromFile(dll));
            }
            catch
            {
                // Native and otherwise unreadable DLLs are simply not references.
            }
        }

        return builder.ToImmutable();
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
            // What the analyzer package itself defaults to when no ini says otherwise.
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
