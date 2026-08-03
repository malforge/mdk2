using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mdk2.PbAnalyzers.Tests.Harness;

/// <summary>
///     Stand-ins for the Space Engineers assemblies, compiled from source so the analyzer tests can run without a game
///     install.
/// </summary>
/// <remarks>
///     The assembly names matter as much as the namespaces: whitelist keys are of the form
///     <c>Some.Namespace.SomeType, AssemblyName</c>, so a stub has to be produced under the same assembly name the real
///     type lives in or the whitelist lookups in the tests would not reflect reality.
/// </remarks>
static class StubGameAssemblies
{
    static readonly object Gate = new();
    static ImmutableArray<MetadataReference> _cached;

    /// <summary>
    ///     Every namespace the programmable block imports on the script's behalf is represented here, so that a script
    ///     using the stock template's using directives compiles against these stubs.
    /// </summary>
    static readonly (string AssemblyName, string Source)[] Stubs =
    [
        ("Sandbox.Common", """
                           namespace Sandbox.ModAPI.Ingame
                           {
                               public class MyGridProgram { public IMyGridTerminalSystem GridTerminalSystem { get; set; } }
                               public interface IMyGridTerminalSystem { }
                               public interface IMyTerminalBlock { string CustomName { get; set; } }
                           }

                           namespace Sandbox.ModAPI
                           {
                               // Deliberately mirrors the ingame interface: this is the namespace people import by mistake.
                               public interface IMyTerminalBlock { string CustomName { get; set; } }
                               public interface IMyEntity { }
                           }

                           namespace Sandbox.ModAPI.Interfaces
                           {
                               public interface ITerminalAction { }
                           }
                           """),

        ("Sandbox.Game", """
                         namespace Sandbox.Game.EntityComponents
                         {
                             public class MyResourceSourceComponent { }
                         }
                         """),

        ("SpaceEngineers.Game", """
                                namespace SpaceEngineers.Game.ModAPI.Ingame
                                {
                                    public interface IMyAirVent { }
                                }

                                namespace SpaceEngineers.Game.ModAPI
                                {
                                    public interface IMyAirVent { }
                                }
                                """),

        ("VRage.Game", """
                       namespace VRage.Game
                       {
                           public struct MyDefinitionId { }
                       }

                       namespace VRage.Game.Components
                       {
                           public class MyComponentBase { }
                       }

                       namespace VRage.Game.GUI.TextPanel
                       {
                           public struct MySprite { }
                       }

                       namespace VRage.Game.ModAPI.Ingame
                       {
                           public interface IMyCubeGrid { }
                       }

                       namespace VRage.Game.ModAPI.Ingame.Utilities
                       {
                           public class MyIni { }
                       }

                       namespace VRage.Game.ModAPI
                       {
                           public interface IMyCubeGrid { }
                       }

                       namespace VRage.Game.ObjectBuilders.Definitions
                       {
                           public class MyObjectBuilder_Definition { }
                       }
                       """),

        ("VRage.Math", """
                       namespace VRageMath
                       {
                           public struct Vector3D { public double X, Y, Z; }
                           public struct Vector2 { public float X, Y; }
                       }
                       """),

        ("VRage.Library", """
                          namespace VRage
                          {
                              public struct MyFixedPoint { }
                          }

                          namespace VRage.Collections
                          {
                              public struct ListReader<T> { }
                          }
                          """)
    ];

    /// <summary>
    ///     The stub assemblies plus the runtime references needed to bind the <c>System</c> namespaces the programmable
    ///     block imports.
    /// </summary>
    public static ImmutableArray<MetadataReference> All
    {
        get
        {
            lock (Gate)
            {
                if (!_cached.IsDefault)
                    return _cached;

                var builder = ImmutableArray.CreateBuilder<MetadataReference>();
                builder.AddRange(RuntimeReferences());
                foreach (var (assemblyName, source) in Stubs)
                    builder.Add(Compile(assemblyName, source, builder.ToImmutable()));

                _cached = builder.ToImmutable();
                return _cached;
            }
        }
    }

    static IEnumerable<MetadataReference> RuntimeReferences()
    {
        // System.Collections.Immutable is one of the namespaces the programmable block imports, so it has to be
        // resolvable or the tests covering the stock template's using directives would pass vacuously.
        var assemblies = new[]
        {
            typeof(object).Assembly,
            typeof(Enumerable).Assembly,
            typeof(ImmutableArray).Assembly,
            typeof(System.Collections.Generic.List<>).Assembly,
            typeof(System.Text.StringBuilder).Assembly,
            typeof(System.Reflection.MemberInfo).Assembly,
            typeof(System.Diagnostics.Stopwatch).Assembly
        };

        var trustedPlatformAssemblies = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        // netstandard and System.Runtime carry the type forwards a lot of the above depend on.
        var required = new[] { "System.Runtime.dll", "netstandard.dll", "System.Collections.dll", "System.Linq.dll" };

        var paths = assemblies.Select(a => a.Location)
            .Concat(trustedPlatformAssemblies.Where(p => required.Contains(Path.GetFileName(p))))
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return paths.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p));
    }

    static MetadataReference Compile(string assemblyName, string source, ImmutableArray<MetadataReference> references)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success)
        {
            var errors = string.Join(Environment.NewLine, result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            throw new InvalidOperationException($"Failed to compile the {assemblyName} stub assembly:{Environment.NewLine}{errors}");
        }

        stream.Position = 0;
        return MetadataReference.CreateFromStream(stream);
    }
}
