using Mdk2.PbAnalyzers.Tests.Harness;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Mdk2.PbAnalyzers.Tests;

/// <summary>
///     MDK06: packing unwraps every namespace the script declares, so a name qualified through one of them has nothing
///     left to resolve against ingame.
/// </summary>
[TestFixture]
public class ScriptNamespaceReferenceTests
{
    [Test]
    public void FullyQualifiedReferenceToAScriptNamespace_IsAnError()
    {
        var result = PbAnalyzerRunner.Run("""
                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  public IngameScript.Helper Self;
                                              }
                                          }
                                          """);

        var diagnostics = result.OfRule("MDK06").ToArray();
        Assert.That(diagnostics, Has.Length.EqualTo(1), result.Describe());
        Assert.That(diagnostics[0].Severity, Is.EqualTo(DiagnosticSeverity.Error));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain("IngameScript"));
    }

    [Test]
    public void PartiallyQualifiedReferenceToASiblingNamespace_IsAnError()
    {
        // The accident that actually happens: this reads like ordinary code and compiles locally.
        var result = PbAnalyzerRunner.Run("""
                                          namespace MyHelpers
                                          {
                                              public class Utils
                                              {
                                                  public static int Clamp(int value) { return value; }
                                              }
                                          }

                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  public int Run() { return MyHelpers.Utils.Clamp(1); }
                                              }
                                          }
                                          """);

        var diagnostics = result.OfRule("MDK06").ToArray();
        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(diagnostics, Has.Length.EqualTo(1), result.Describe());

        var location = diagnostics[0].Location;
        Assert.That(location.SourceTree!.GetText().ToString(location.SourceSpan), Is.EqualTo("MyHelpers"));
    }

    [Test]
    public void QualifiedReferenceInTypePosition_ReportsOnceAcrossTheWholeQualifier()
    {
        var result = PbAnalyzerRunner.Run("""
                                          namespace Deep.Nested.Space
                                          {
                                              public class Thing { }
                                          }

                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  public Deep.Nested.Space.Thing Item;
                                              }
                                          }
                                          """);

        var diagnostics = result.OfRule("MDK06").ToArray();
        Assert.That(diagnostics, Has.Length.EqualTo(1), $"a multi-segment qualifier is one mistake, not three:\n{result.Describe()}");

        var location = diagnostics[0].Location;
        Assert.That(location.SourceTree!.GetText().ToString(location.SourceSpan), Is.EqualTo("Deep.Nested.Space"));
    }

    [Test]
    public void QualifiedReferenceInExpressionPosition_ReportsOnce()
    {
        var result = PbAnalyzerRunner.Run("""
                                          namespace Deep.Nested.Space
                                          {
                                              public class Thing
                                              {
                                                  public static int Value() { return 1; }
                                              }
                                          }

                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  public int Run() { return Deep.Nested.Space.Thing.Value(); }
                                              }
                                          }
                                          """);

        var diagnostics = result.OfRule("MDK06").ToArray();
        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(diagnostics, Has.Length.EqualTo(1), result.Describe());

        // In expression position a multi-segment namespace is member access rather than a qualified name, and it still
        // has to be reported once, across the whole qualifier.
        var location = diagnostics[0].Location;
        Assert.That(location.SourceTree!.GetText().ToString(location.SourceSpan), Is.EqualTo("Deep.Nested.Space"));
    }

    [Test]
    public void GlobalAliasQualifiedReference_IsAnError()
    {
        var result = PbAnalyzerRunner.Run("""
                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  public global::IngameScript.Helper Self;
                                              }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK06").Count(), Is.EqualTo(1), result.Describe());
    }

    [Test]
    public void NameofOfAScriptNamespace_IsAnError()
    {
        var result = PbAnalyzerRunner.Run("""
                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  public string Name() { return nameof(IngameScript); }
                                              }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK06").Count(), Is.EqualTo(1), result.Describe());
    }

    [Test]
    public void ReferenceAcrossFiles_IsAnError()
    {
        var result = PbAnalyzerRunner.Run([
            new SourceFile("Utils.cs", """
                                       namespace MyHelpers
                                       {
                                           public class Utils { }
                                       }
                                       """),
            new SourceFile("Program.cs", """
                                         namespace IngameScript
                                         {
                                             public class Helper
                                             {
                                                 public MyHelpers.Utils Item;
                                             }
                                         }
                                         """)
        ]);

        Assert.That(result.OfRule("MDK06").Count(), Is.EqualTo(1), result.Describe());
    }

    [Test]
    public void NamespaceDeclaration_IsNotAReference()
    {
        var result = PbAnalyzerRunner.Run("""
                                          namespace IngameScript
                                          {
                                              public class Helper { }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK06"), Is.Empty, $"declaring a namespace is not referencing one:\n{result.Describe()}");
    }

    [Test]
    public void DottedNamespaceDeclaration_IsNotAReference()
    {
        var result = PbAnalyzerRunner.Run("""
                                          namespace IngameScript.Helpers
                                          {
                                              public class Helper { }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK06"), Is.Empty, result.Describe());
    }

    [Test]
    public void NestedNamespaceDeclaration_IsNotAReference()
    {
        var result = PbAnalyzerRunner.Run("""
                                          namespace IngameScript
                                          {
                                              namespace Helpers
                                              {
                                                  public class Helper { }
                                              }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK06"), Is.Empty, result.Describe());
    }

    [Test]
    public void UsingDirective_IsNotAReference()
    {
        // MDK05 owns using directives, and it deliberately says nothing about the script's own namespaces.
        var result = PbAnalyzerRunner.Run("""
                                          using MyHelpers;

                                          namespace MyHelpers
                                          {
                                              public class Utils { }
                                          }

                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  public Utils Item;
                                              }
                                          }
                                          """);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK06"), Is.Empty, $"importing then using the short name is the correct fix:\n{result.Describe()}");
    }

    [Test]
    public void QualifiedReferenceToAGameNamespace_IsNotAnError()
    {
        var result = PbAnalyzerRunner.Run("""
                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  public VRageMath.Vector3D Position;
                                              }
                                          }
                                          """);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK06"), Is.Empty, $"game namespaces survive packing:\n{result.Describe()}");
    }

    [Test]
    public void QualifiedReferenceToAFrameworkNamespace_IsNotAnError()
    {
        var result = PbAnalyzerRunner.Run("""
                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  public System.Collections.Generic.List<int> Items;
                                              }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK06"), Is.Empty, result.Describe());
    }

    [Test]
    public void NamespaceSharedWithTheGame_IsNotReported()
    {
        // A script may extend an existing namespace. The namespace itself still exists after packing, so the
        // qualification keeps working, and reporting it would be a false positive.
        var result = PbAnalyzerRunner.Run("""
                                          namespace VRageMath
                                          {
                                              public class Extra { }
                                          }

                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  public VRageMath.Extra Item;
                                              }
                                          }
                                          """);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK06"), Is.Empty, result.Describe());
    }

    [Test]
    public void UnqualifiedCodeInASingleNamespace_IsNotReported()
    {
        var result = PbAnalyzerRunner.Run("""
                                          using Sandbox.ModAPI.Ingame;

                                          namespace IngameScript
                                          {
                                              public partial class Program : MyGridProgram
                                              {
                                                  readonly Helper _helper = new Helper();
                                                  public void Main() { _helper.Run(); }
                                              }

                                              public class Helper
                                              {
                                                  public void Run() { }
                                              }
                                          }
                                          """);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK06"), Is.Empty, $"ordinary script code must stay clean:\n{result.Describe()}");
    }

    [Test]
    public void IgnoredFile_IsNotReported()
    {
        var result = PbAnalyzerRunner.Run(
            [new SourceFile("Generated/Stuff.cs", """
                                                  namespace IngameScript
                                                  {
                                                      public class Helper
                                                      {
                                                          public IngameScript.Helper Self;
                                                      }
                                                  }
                                                  """)],
            """
            [mdk]
            ignores=Generated/**/*.cs
            """);

        Assert.That(result.OfRule("MDK06"), Is.Empty, $"ignored paths must stay ignored:\n{result.Describe()}");
    }

    [Test]
    public void QualifiedReferenceInsideATypeArgument_IsAnError()
    {
        var result = PbAnalyzerRunner.Run("""
                                          namespace MyHelpers
                                          {
                                              public class Utils { }
                                          }

                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  public System.Collections.Generic.List<MyHelpers.Utils> Items;
                                              }
                                          }
                                          """);

        var diagnostics = result.OfRule("MDK06").ToArray();
        Assert.That(diagnostics, Has.Length.EqualTo(1), $"only the script namespace is a problem, not System.Collections.Generic:\n{result.Describe()}");

        var location = diagnostics[0].Location;
        Assert.That(location.SourceTree!.GetText().ToString(location.SourceSpan), Is.EqualTo("MyHelpers"));
    }

    [Test]
    public void QualifiedReferenceInTypeof_IsAnError()
    {
        var result = PbAnalyzerRunner.Run("""
                                          namespace MyHelpers
                                          {
                                              public class Utils { }
                                          }

                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  public System.Type Which() { return typeof(MyHelpers.Utils); }
                                              }
                                          }
                                          """);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK06").Count(), Is.EqualTo(1), result.Describe());
    }

    [Test]
    public void QualifiedReferenceInAnAttribute_IsAnError()
    {
        var result = PbAnalyzerRunner.Run("""
                                          namespace MyHelpers
                                          {
                                              public class MarkAttribute : System.Attribute { }
                                          }

                                          namespace IngameScript
                                          {
                                              [MyHelpers.Mark]
                                              public class Helper { }
                                          }
                                          """);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK06").Count(), Is.EqualTo(1), result.Describe());
    }

    [Test]
    public void QualifiedReferenceThroughANamespaceAlias_IsAnError()
    {
        // The alias does not survive packing either, and what it points at is gone as well.
        var result = PbAnalyzerRunner.Run("""
                                          using MH = MyHelpers;

                                          namespace MyHelpers
                                          {
                                              public class Utils { }
                                          }

                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  public MH.Utils Item;
                                              }
                                          }
                                          """);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK06").Count(), Is.EqualTo(1), result.Describe());
    }

    [Test]
    public void UsingInsideANamespaceBlock_IsNotAReference()
    {
        var result = PbAnalyzerRunner.Run("""
                                          namespace MyHelpers
                                          {
                                              public class Utils { }
                                          }

                                          namespace IngameScript
                                          {
                                              using MyHelpers;

                                              public class Helper
                                              {
                                                  public Utils Item;
                                              }
                                          }
                                          """);

        Assert.That(result.CompilerErrors, Is.Empty, result.Describe());
        Assert.That(result.OfRule("MDK06"), Is.Empty, result.Describe());
    }

    [Test]
    public void SeveralDistinctReferences_EachReport()
    {
        var result = PbAnalyzerRunner.Run("""
                                          namespace MyHelpers
                                          {
                                              public class Utils { }
                                          }

                                          namespace IngameScript
                                          {
                                              public class Helper
                                              {
                                                  public MyHelpers.Utils First;
                                                  public MyHelpers.Utils Second;
                                                  public IngameScript.Helper Third;
                                              }
                                          }
                                          """);

        Assert.That(result.OfRule("MDK06").Count(), Is.EqualTo(3), result.Describe());
    }
}
