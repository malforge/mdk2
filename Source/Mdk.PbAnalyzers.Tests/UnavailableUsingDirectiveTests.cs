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
    public void NamespaceReachedThroughTheBlocksOwnAliases_DoesNotWarn()
    {
        // The programmable block declares aliases for six System.ComponentModel types rather than importing the
        // namespace, so this code is fine. Missing that mechanism is what made an earlier version of this rule report a
        // working script.
        var result = PbAnalyzerRunner.Run("""
                                          using System.ComponentModel;

                                          namespace IngameScript
                                          {
                                              public class Helper : INotifyPropertyChanged
                                              {
                                                  public event PropertyChangedEventHandler PropertyChanged;
                                              }
                                          }
                                          """);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK05"), Is.Empty, $"those types are aliased by the block itself:\n{result.Describe()}");
    }

    [Test]
    public void AliasRestatingOneTheBlockDeclares_DoesNotWarn()
    {
        var result = PbAnalyzerRunner.Run("""
                                          using INotifyPropertyChanged = System.ComponentModel.INotifyPropertyChanged;

                                          namespace IngameScript
                                          {
                                              public class Helper : INotifyPropertyChanged
                                              {
                                                  public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
                                              }
                                          }
                                          """);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK05"), Is.Empty, $"the block declares this very alias:\n{result.Describe()}");
    }

    [TestCase("System.Text.RegularExpressions", "Regex")]
    [TestCase("System.Globalization", "CultureInfo")]
    public void WhitelistedButUnimportedNamespace_Warns(string ns, string type)
    {
        // The sharp end of this rule: fully whitelisted, so nothing else objects, but not imported by the block, so the
        // packed script does not compile.
        var result = PbAnalyzerRunner.Run($$"""
                                           using {{ns}};

                                           namespace IngameScript
                                           {
                                               public class Helper { public {{type}} Thing; }
                                           }
                                           """);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK05").Count(), Is.EqualTo(1), result.Describe());
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

    [TestCase("Sandbox.ModAPI", "Sandbox.ModAPI.Ingame", "IMyEntity")]
    [TestCase("VRage.Game.ModAPI", "VRage.Game.ModAPI.Ingame", "IMyCubeGrid")]
    [TestCase("SpaceEngineers.Game.ModAPI", "SpaceEngineers.Game.ModAPI.Ingame", "IMyAirVent")]
    public void ModApiNamespace_WarnsAndSuggestsTheIngameNamespace(string modApi, string ingame, string type)
    {
        var result = PbAnalyzerRunner.Run($$"""
                                           using {{modApi}};

                                           namespace IngameScript
                                           {
                                               public class Helper { public {{type}} Thing; }
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
                                              public class Helper { public MemberInfo Info; }
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
                                              public class Helper { public IMyEntity Thing; }
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
                                              public class Helper { public Block Target; }
                                          }
                                          """);

        var diagnostics = result.OfRule("MDK05").ToArray();
        Assert.That(diagnostics, Has.Length.EqualTo(1), result.Describe());
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("The alias 'Block'"));

        var location = diagnostics[0].Location;
        Assert.That(location.SourceTree!.GetText().ToString(location.SourceSpan), Is.EqualTo("Block"));
    }

    [Test]
    public void AliasRestatingATypesOwnName_DoesNotWarn()
    {
        // Written to keep the mod API and the ingame API apart while editing. The block imports the ingame namespace,
        // so the packed script resolves the plain name to the same type and losing the alias changes nothing. Found in
        // a shared mixin, so it travels into other people's projects.
        var result = PbAnalyzerRunner.Run("""
                                          using IMyTerminalBlock = Sandbox.ModAPI.Ingame.IMyTerminalBlock;

                                          namespace IngameScript
                                          {
                                              public class Helper { public IMyTerminalBlock Target; }
                                          }
                                          """);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK05"), Is.Empty, $"the block provides that name anyway:\n{result.Describe()}");
    }

    [Test]
    public void AliasRenamingAType_StillWarns()
    {
        // A genuinely different name has nothing to fall back on once the directive is gone.
        var result = PbAnalyzerRunner.Run("""
                                          using Block = Sandbox.ModAPI.Ingame.IMyTerminalBlock;

                                          namespace IngameScript
                                          {
                                              public class Helper { public Block Target; }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK05").Count(), Is.EqualTo(1), result.Describe());
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
                                              public class Helper { public MH.Utils Item; }
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
                                              public class Helper { public double Root() { return Sqrt(2.0); } }
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
                                              public class Helper { public int Value() { return Zero(); } }
                                          }
                                          """);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK05").Count(), Is.EqualTo(1), result.Describe());
    }

    [Test]
    public void ImportKeptBesideFullyQualifiedUse_DoesNotWarn()
    {
        // Writing the type out in full is the correct way to use an unimported namespace, and people often leave the
        // now-pointless using behind. The member call through ?. must not be mistaken for needing the import.
        var result = PbAnalyzerRunner.Run("""
                                          using System.Text.RegularExpressions;

                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  readonly System.Text.RegularExpressions.Regex _regex = null;
                                                  public bool IsMatch(string input) { return _regex.IsMatch(input); }
                                              }
                                          }
                                          """);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK05"), Is.Empty, $"the qualified code does not need the import:\n{result.Describe()}");
    }

    [Test]
    public void ImportUsedOnlyThroughANamedArgument_DoesNotWarn()
    {
        // A parameter name is not a type reference, so it cannot be what makes an import necessary.
        var result = PbAnalyzerRunner.Run("""
                                          using System.Globalization;

                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  public string Format(double value)
                                                  {
                                                      return value.ToString(provider: System.Globalization.CultureInfo.InvariantCulture);
                                                  }
                                              }
                                          }
                                          """);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK05"), Is.Empty, result.Describe());
    }

    [Test]
    public void UnusedImport_DoesNotWarn()
    {
        // Old templates and stock Visual Studio file headers seed imports nothing uses. Nothing depends on them, so
        // nothing breaks when packing drops them, and warning about them would be noise across the whole ecosystem.
        var result = PbAnalyzerRunner.Run("""
                                          using System.Threading.Tasks;

                                          namespace IngameScript
                                          {
                                              public class Helper { }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK05"), Is.Empty, $"an unused import harms nothing:\n{result.Describe()}");
    }

    [Test]
    public void RedundantStaticImportOfTheEnclosingClass_DoesNotWarn()
    {
        // A class nested inside Program reaches Program's members through the nesting, so this static import is
        // redundant already. Packing nests it further rather than breaking it. Seen in the wild.
        var result = PbAnalyzerRunner.Run([
            new SourceFile("Program.cs", """
                                         using Sandbox.ModAPI.Ingame;

                                         namespace IngameScript
                                         {
                                             public partial class Program : MyGridProgram
                                             {
                                                 public enum Property { Auto }
                                                 public static void Register(Property property) { }
                                             }
                                         }
                                         """),
            new SourceFile("Handler.cs", """
                                         using static IngameScript.Program;

                                         namespace IngameScript
                                         {
                                             public partial class Program
                                             {
                                                 public class Handler
                                                 {
                                                     public Handler() { Register(Property.Auto); }
                                                 }
                                             }
                                         }
                                         """)
        ]);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK05"), Is.Empty, $"redundant imports are not breakage:\n{result.Describe()}");
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
                                                      public class Helper { public IMyEntity Thing; }
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
                                              public class Helper
                                              {
                                                  public IMyEntity Entity;
                                                  public IMyCubeGrid Grid;
                                                  public MemberInfo Info;
                                              }
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
                                              public class Helper { public IMyEntity Thing; }
                                          }
                                          """);

        var location = result.OfRule("MDK05").Single().Location;
        var text = location.SourceTree!.GetText().ToString(location.SourceSpan);
        Assert.That(text, Is.EqualTo("Sandbox.ModAPI"), "the squiggle belongs under the namespace, not the whole directive");
    }
}
