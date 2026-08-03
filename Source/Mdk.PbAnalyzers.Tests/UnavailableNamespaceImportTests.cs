using Mdk2.PbAnalyzers.Tests.Harness;
using NUnit.Framework;

namespace Mdk2.PbAnalyzers.Tests;

/// <summary>
///     MDK05: the programmable block imports a fixed set of namespaces, and packing strips every using directive from
///     the script. Importing anything else means the packed script loses the import.
/// </summary>
[TestFixture]
public class UnavailableNamespaceImportTests
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
    public void AliasUsing_IsLeftAlone()
    {
        var result = PbAnalyzerRunner.Run("""
                                          using Block = Sandbox.ModAPI.Ingame.IMyTerminalBlock;

                                          namespace IngameScript
                                          {
                                              public class Helper { }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK05"), Is.Empty, $"alias usings are out of this rule's scope:\n{result.Describe()}");
    }

    [Test]
    public void StaticUsing_IsLeftAlone()
    {
        var result = PbAnalyzerRunner.Run("""
                                          using static System.Math;

                                          namespace IngameScript
                                          {
                                              public class Helper { }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK05"), Is.Empty, $"static usings are out of this rule's scope:\n{result.Describe()}");
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
