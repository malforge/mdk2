using Mdk2.PbAnalyzers.Tests.Harness;
using NUnit.Framework;

namespace Mdk2.PbAnalyzers.Tests;

/// <summary>
///     MDK05: packing strips every using directive from the script, and the programmable block only reinstates a fixed
///     set of namespace imports. Everything else - other namespaces, aliases, static imports - is simply lost.
/// </summary>
[TestFixture]
public class UnavailableUsingDirectiveTests
{
    /// <summary>
    ///     The using directives the script template ships with. Not one of them may warn, or every new project would
    ///     start out with warnings.
    /// </summary>
    const string TemplateUsings = """
                                  using Sandbox.Game.EntityComponents;
                                  using Sandbox.ModAPI.Ingame;
                                  using Sandbox.ModAPI.Interfaces;
                                  using SpaceEngineers.Game.ModAPI.Ingame;
                                  using System;
                                  using System.Collections;
                                  using System.Collections.Generic;
                                  using System.Collections.Immutable;
                                  using System.Linq;
                                  using System.Text;
                                  using VRage;
                                  using VRage.Collections;
                                  using VRage.Game;
                                  using VRage.Game.Components;
                                  using VRage.Game.GUI.TextPanel;
                                  using VRage.Game.ModAPI.Ingame;
                                  using VRage.Game.ModAPI.Ingame.Utilities;
                                  using VRage.Game.ObjectBuilders.Definitions;
                                  using VRageMath;
                                  """;

    [Test]
    public void TemplateUsings_DoNotWarn()
    {
        var result = PbAnalyzerRunner.Run($$"""
                                           {{TemplateUsings}}

                                           namespace IngameScript
                                           {
                                               public partial class Program : MyGridProgram
                                               {
                                               }
                                           }
                                           """);

        // If a namespace failed to bind, MDK05 would be silent for the wrong reason and this test would prove nothing.
        Assert.That(result.CompilerErrors, Is.Empty, $"the template should compile against the stubs:\n{result.Describe()}");
        Assert.That(result.OfRule("MDK05"), Is.Empty, $"the stock template must not warn:\n{result.Describe()}");
    }

    [Test]
    public void MemorySafeTypes_DoesNotWarn()
    {
        // The programmable block imports this one even though the template never mentions it. It is exactly what a
        // hand-maintained list gets wrong, which is why the list is extracted from the game instead.
        var result = PbAnalyzerRunner.Run("""
                                          using VRage.Scripting.MemorySafeTypes;

                                          namespace IngameScript
                                          {
                                              public class Helper { }
                                          }
                                          """);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK05"), Is.Empty, result.Describe());
    }

    [TestCase("Sandbox.ModAPI", "Sandbox.ModAPI.Ingame")]
    [TestCase("VRage.Game.ModAPI", "VRage.Game.ModAPI.Ingame")]
    [TestCase("SpaceEngineers.Game.ModAPI", "SpaceEngineers.Game.ModAPI.Ingame")]
    public void ModApiNamespace_WarnsAndSuggestsTheIngameNamespace(string modApi, string ingame)
    {
        var result = PbAnalyzerRunner.Run($$"""
                                           using {{modApi}};

                                           namespace IngameScript
                                           {
                                               public class Helper { }
                                           }
                                           """);

        var diagnostics = result.OfRule("MDK05").ToArray();
        Assert.That(diagnostics, Has.Length.EqualTo(1), $"expected exactly one MDK05:\n{result.Describe()}");
        Assert.That(diagnostics[0].GetMessage(), Does.Contain(modApi));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain($"Did you mean '{ingame}'?"),
            "the mod API mix-up is the whole point of the rule, so the ingame namespace has to be suggested");
    }

    [Test]
    public void IngameNamespace_DoesNotWarn()
    {
        var result = PbAnalyzerRunner.Run("""
                                          using Sandbox.ModAPI.Ingame;

                                          namespace IngameScript
                                          {
                                              public class Helper { }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK05"), Is.Empty, result.Describe());
    }

    [Test]
    public void NamespaceOutsideTheImportedSet_WarnsWithoutASuggestion()
    {
        // System.Reflection holds whitelisted members, but the programmable block does not import it, so the types have
        // to be written out in full.
        var result = PbAnalyzerRunner.Run("""
                                          using System.Reflection;

                                          namespace IngameScript
                                          {
                                              public class Helper { }
                                          }
                                          """);

        var diagnostics = result.OfRule("MDK05").ToArray();
        Assert.That(diagnostics, Has.Length.EqualTo(1), result.Describe());
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("System.Reflection"));
        Assert.That(diagnostics[0].GetMessage(), Does.Not.Contain("Did you mean"),
            "there is no System.Reflection.Ingame to suggest");
    }

    [Test]
    public void Severity_IsWarning()
    {
        var result = PbAnalyzerRunner.Run("""
                                          using Sandbox.ModAPI;

                                          namespace IngameScript
                                          {
                                              public class Helper { }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK05").Single().Severity, Is.EqualTo(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning));
    }

    [Test]
    public void ImportOfAScriptDeclaredNamespace_DoesNotWarn()
    {
        // Packing unwraps the script's own namespaces, so this import is redundant rather than broken.
        var result = PbAnalyzerRunner.Run("""
                                          using MyHelpers;

                                          namespace MyHelpers
                                          {
                                              public class Utils { }
                                          }

                                          namespace IngameScript
                                          {
                                              public class Helper { }
                                          }
                                          """);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK05"), Is.Empty, $"the script's own namespaces survive as flattened code:\n{result.Describe()}");
    }

    [Test]
    public void ImportOfAScriptDeclaredNamespaceInAnotherFile_DoesNotWarn()
    {
        var result = PbAnalyzerRunner.Run([
            new SourceFile("Program.cs", """
                                         using MyHelpers;

                                         namespace IngameScript
                                         {
                                             public class Helper { }
                                         }
                                         """),
            new SourceFile("Utils.cs", """
                                       namespace MyHelpers
                                       {
                                           public class Utils { }
                                       }
                                       """)
        ]);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK05"), Is.Empty, result.Describe());
    }

    [Test]
    public void AliasUsing_Warns()
    {
        // The alias exists nowhere but in the directive, so packing takes the name with it even though what it points at
        // is a perfectly legal ingame type.
        var result = PbAnalyzerRunner.Run("""
                                          using Block = Sandbox.ModAPI.Ingame.IMyTerminalBlock;

                                          namespace IngameScript
                                          {
                                              public class Helper { }
                                          }
                                          """);

        var diagnostics = result.OfRule("MDK05").ToArray();
        Assert.That(diagnostics, Has.Length.EqualTo(1), result.Describe());
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("The alias 'Block'"));

        var location = diagnostics[0].Location;
        Assert.That(location.SourceTree!.GetText().ToString(location.SourceSpan), Is.EqualTo("Block"));
    }

    [Test]
    public void AliasUsingForAScriptNamespace_Warns()
    {
        // The namespace survives as flattened code, but the alias still does not.
        var result = PbAnalyzerRunner.Run("""
                                          using MH = MyHelpers;

                                          namespace MyHelpers
                                          {
                                              public class Utils { }
                                          }

                                          namespace IngameScript
                                          {
                                              public class Helper { }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK05").Count(), Is.EqualTo(1), result.Describe());
    }

    [Test]
    public void StaticUsing_Warns()
    {
        var result = PbAnalyzerRunner.Run("""
                                          using static System.Math;

                                          namespace IngameScript
                                          {
                                              public class Helper { }
                                          }
                                          """);

        var diagnostics = result.OfRule("MDK05").ToArray();
        Assert.That(diagnostics, Has.Length.EqualTo(1), result.Describe());
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("The static import of 'System.Math'"));
    }

    [Test]
    public void StaticUsingOfAScriptType_Warns()
    {
        // The programmable block imports namespaces, never types, so nothing reinstates a static import.
        var result = PbAnalyzerRunner.Run("""
                                          using static MyHelpers.Utils;

                                          namespace MyHelpers
                                          {
                                              public class Utils
                                              {
                                                  public static int Zero() { return 0; }
                                              }
                                          }

                                          namespace IngameScript
                                          {
                                              public class Helper { }
                                          }
                                          """);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK05").Count(), Is.EqualTo(1), result.Describe());
    }

    [Test]
    public void UnresolvableNamespace_IsLeftToTheCompiler()
    {
        var result = PbAnalyzerRunner.Run("""
                                          using Some.Namespace.That.Does.Not.Exist;

                                          namespace IngameScript
                                          {
                                              public class Helper { }
                                          }
                                          """);

        Assert.That(result.CompilerErrors.Select(d => d.Id), Does.Contain("CS0246").Or.Contain("CS0234"),
            "the scenario is only meaningful if the compiler already complains");
        Assert.That(result.OfRule("MDK05"), Is.Empty, "no point piling on top of a compiler error");
    }

    [Test]
    public void IgnoredFile_DoesNotWarn()
    {
        var result = PbAnalyzerRunner.Run(
            [new SourceFile("Generated/Stuff.cs", """
                                                  using Sandbox.ModAPI;

                                                  namespace IngameScript
                                                  {
                                                      public class Helper { }
                                                  }
                                                  """)],
            """
            [mdk]
            ignores=Generated/**/*.cs
            """);

        Assert.That(result.OfRule("MDK05"), Is.Empty, $"ignored paths must stay ignored:\n{result.Describe()}");
    }

    [Test]
    public void MultipleBadImports_EachWarnOnce()
    {
        var result = PbAnalyzerRunner.Run("""
                                          using Sandbox.ModAPI;
                                          using VRage.Game.ModAPI;
                                          using System.Reflection;

                                          namespace IngameScript
                                          {
                                              public class Helper { }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK05").Count(), Is.EqualTo(3), result.Describe());
    }

    [Test]
    public void Diagnostic_PointsAtTheNamespaceName()
    {
        var result = PbAnalyzerRunner.Run("""
                                          using Sandbox.ModAPI;

                                          namespace IngameScript
                                          {
                                              public class Helper { }
                                          }
                                          """);

        var location = result.OfRule("MDK05").Single().Location;
        var text = location.SourceTree!.GetText().ToString(location.SourceSpan);
        Assert.That(text, Is.EqualTo("Sandbox.ModAPI"), "the squiggle belongs under the namespace, not the whole directive");
    }
}
