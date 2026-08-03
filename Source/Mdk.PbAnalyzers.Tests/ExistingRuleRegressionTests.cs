using Mdk2.PbAnalyzers.Tests.Harness;
using NUnit.Framework;

namespace Mdk2.PbAnalyzers.Tests;

/// <summary>
///     MDK05 and MDK06 hook into the same syntax walk the older rules use, so those rules are pinned down here.
/// </summary>
[TestFixture]
public class ExistingRuleRegressionTests
{
    [Test]
    public void ProhibitedType_StillReportsMdk01()
    {
        // Sandbox.ModAPI is the mod API: whitelisted for mods, not for scripts.
        var result = PbAnalyzerRunner.Run("""
                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  public Sandbox.ModAPI.IMyEntity Entity;
                                              }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK01").Count(), Is.EqualTo(1), result.Describe());
    }

    [Test]
    public void WhitelistedType_StillReportsNothing()
    {
        var result = PbAnalyzerRunner.Run("""
                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  public Sandbox.ModAPI.Ingame.IMyTerminalBlock Block;
                                              }
                                          }
                                          """);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK01"), Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK06"), Is.Empty, result.Describe());
    }

    [Test]
    public void UsingDirective_DoesNotReportMdk01()
    {
        // Namespaces have never been whitelist-checked, and MDK05 is what covers imports now.
        var result = PbAnalyzerRunner.Run("""
                                          using Sandbox.ModAPI;

                                          namespace IngameScript
                                          {
                                              public class Helper { }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK01"), Is.Empty, $"MDK05 is the rule for imports:\n{result.Describe()}");
        Assert.That(result.OfRule("MDK05").Count(), Is.EqualTo(1), result.Describe());
    }

    [Test]
    public void Dynamic_StillReportsMdk02()
    {
        var result = PbAnalyzerRunner.Run("""
                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  public dynamic Loose;
                                              }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK02").Count(), Is.EqualTo(1), result.Describe());
    }

    [Test]
    public void Destructor_StillReportsMdk02()
    {
        var result = PbAnalyzerRunner.Run("""
                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  ~Helper() { }
                                              }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK02").Count(), Is.EqualTo(1), result.Describe());
    }

    [Test]
    public void ClassOutsideTheAllowedNamespace_StillReportsMdk03()
    {
        var result = PbAnalyzerRunner.Run("""
                                          namespace SomewhereElse
                                          {
                                              public class Helper { }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK03").Count(), Is.EqualTo(1), result.Describe());
    }

    [Test]
    public void ClassInTheAllowedNamespace_StillReportsNoMdk03()
    {
        var result = PbAnalyzerRunner.Run("""
                                          namespace IngameScript
                                          {
                                              public class Helper { }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK03"), Is.Empty, result.Describe());
    }

    [Test]
    public void ConfiguredNamespaces_StillHonouredByMdk03()
    {
        var result = PbAnalyzerRunner.Run(
            [new SourceFile("Program.cs", """
                                          namespace MyHelpers
                                          {
                                              public class Utils { }
                                          }

                                          namespace IngameScript
                                          {
                                              public class Helper { }
                                          }
                                          """)],
            """
            [mdk]
            namespaces=IngameScript,MyHelpers
            """);

        Assert.That(result.OfRule("MDK03"), Is.Empty, $"both namespaces are configured as allowed:\n{result.Describe()}");
    }

    [Test]
    public void ConfiguredNamespaces_DoNotExemptQualifiedReferences()
    {
        // An allowed namespace is still deleted during packing, so MDK06 applies regardless of the ini setting.
        var result = PbAnalyzerRunner.Run(
            [new SourceFile("Program.cs", """
                                          namespace MyHelpers
                                          {
                                              public class Utils { }
                                          }

                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  public MyHelpers.Utils Item;
                                              }
                                          }
                                          """)],
            """
            [mdk]
            namespaces=IngameScript,MyHelpers
            """);

        Assert.That(result.OfRule("MDK03"), Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK06").Count(), Is.EqualTo(1), result.Describe());
    }

    [Test]
    public void RealisticScript_ProducesNoDiagnosticsAtAll()
    {
        // Only game types are exercised here. The whitelist keys framework types against the .NET Framework assembly
        // names that programmable block projects build with, whereas this fixture runs on .NET, where those same types
        // come from differently named assemblies. Framework types would therefore trip MDK01 for reasons that have
        // nothing to do with the script. The framework namespaces are still imported below, which is what the new rules
        // care about.
        var result = PbAnalyzerRunner.Run("""
                                          using Sandbox.ModAPI.Ingame;
                                          using System.Collections.Generic;
                                          using VRage.Game.ModAPI.Ingame.Utilities;
                                          using VRageMath;

                                          namespace IngameScript
                                          {
                                              public partial class Program : MyGridProgram
                                              {
                                                  readonly MyIni _ini = new MyIni();
                                                  readonly IMyTerminalBlock[] _blocks = new IMyTerminalBlock[8];
                                                  Vector3D _position;

                                                  public void Main(string argument)
                                                  {
                                                      _blocks[0] = null;
                                                      _position = new Vector3D();
                                                  }
                                              }
                                          }
                                          """);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.Analyzer, Is.Empty, $"a plain, correct script must be silent:\n{result.Describe()}");
    }
}
