using System.Collections.Immutable;
using FakeItEasy;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;
using Mdk.CommandLine.Shared;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptPostProcessors;

[TestFixture]
public class SymbolProtectionAnnotatorTests : DocumentProcessorTests<RegionAnnotator>
{
    [Test]
    public async Task ProcessAsync_AnnotatesProtectedMembers()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            class Program: MyGridProgram
            {
                public Program()
                {
                }

                public void Save()
                {
                }

                private void Save(string withWhatever)
                {
                }

                protected void Save(int withWhatever)
                {
                }

                public void Main(string argument, UpdateType updateSource)
                {
                }

                private void Main(string argument)
                {
                }

                protected void Main()
                {
                }

                public void Main(int withWhatever)
                {
                }

                public void UnprotectedMethod()
                {
                }

                public string UnprotectedProperty { get; set; }

                private string _unprotectedField;

                public class UnprotectedNestedClass
                {
                    public void Main(string argument, UpdateType updateSource)
                    {
                    }

                    public void Save()
                    {
                    }
                }
            }

            class UnprotectedClass
            {
                public void Main(string argument, UpdateType updateSource)
                {
                }

                public void Save()
                {
                }

                public class Program
                {
                    public Program()
                    {
                    }

                    public void Save()
                    {
                    }

                    public void Main(string argument, UpdateType updateSource)
                    {
                    }
                }
            }
            """);

        var processor = new SymbolProtectionAnnotator();
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb =
            {
                MinifierLevel = MinifierLevel.None,
                ProjectFile = @"A:\Fake\Path\Project.csproj",
                Output = @"A:\Fake\Path\Output"
            }
        };
        var context = new PackContext(
            parameters,
            A.Fake<IConsole>(o => o.Strict()),
            A.Fake<IInteraction>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        // Act
        var result = processor.ProcessAsync(document, context).Result;

        // Assert
        Assert.That(result, Is.Not.SameAs(document));
        var root = await result.GetSyntaxRootAsync();
        Assert.That(root, Is.Not.Null);

        var programClass = root!.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "Program");
        var annotations = programClass.GetAnnotations("MDK").ToList();
        Assert.That(annotations, Is.Not.Empty);
        Assert.That(annotations.Count, Is.EqualTo(1));
        Assert.That(annotations[0].Data, Is.EqualTo("preserve"));

        var saveMethods = programClass.Members.OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.Text == "Save").ToList();
        Assert.That(saveMethods, Has.Count.EqualTo(3));
        foreach (var saveMethod in saveMethods)
        {
            annotations = saveMethod.GetAnnotations("MDK").ToList();
            Assert.That(annotations, Is.Not.Empty);
            Assert.That(annotations.Count, Is.EqualTo(1));
            Assert.That(annotations[0].Data, Is.EqualTo("preserve"));
        }

        var mainMethods = programClass.Members.OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.Text == "Main").ToList();
        Assert.That(mainMethods, Has.Count.EqualTo(4));
        foreach (var mainMethod in mainMethods)
        {
            annotations = mainMethod.GetAnnotations("MDK").ToList();
            Assert.That(annotations, Is.Not.Empty);
            Assert.That(annotations.Count, Is.EqualTo(1));
            Assert.That(annotations[0].Data, Is.EqualTo("preserve"));
        }

        var unprotectedMethod = programClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "UnprotectedMethod");
        annotations = unprotectedMethod.GetAnnotations("MDK").ToList();
        Assert.That(annotations, Is.Empty);

        var unprotectedProperty = programClass.Members.OfType<PropertyDeclarationSyntax>().First(p => p.Identifier.Text == "UnprotectedProperty");
        annotations = unprotectedProperty.GetAnnotations("MDK").ToList();
        Assert.That(annotations, Is.Empty);

        var unprotectedField = programClass.Members.OfType<FieldDeclarationSyntax>().First(f => f.Declaration.Variables.First().Identifier.Text == "_unprotectedField");
        annotations = unprotectedField.GetAnnotations("MDK").ToList();
        Assert.That(annotations, Is.Empty);

        var unprotectedNestedClass = programClass.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "UnprotectedNestedClass");
        annotations = unprotectedNestedClass.GetAnnotations("MDK").ToList();
        Assert.That(annotations, Is.Empty);

        var unprotectedNestedSaveMethod = unprotectedNestedClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Save");
        annotations = unprotectedNestedSaveMethod.GetAnnotations("MDK").ToList();
        Assert.That(annotations, Is.Empty);

        var unprotectedNestedMainMethod = unprotectedNestedClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Main");
        annotations = unprotectedNestedMainMethod.GetAnnotations("MDK").ToList();
        Assert.That(annotations, Is.Empty);

        var unprotectedClass = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "UnprotectedClass");
        annotations = unprotectedClass.GetAnnotations("MDK").ToList();
        Assert.That(annotations, Is.Empty);

        var unprotectedClassMainMethod = unprotectedClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Main");
        annotations = unprotectedClassMainMethod.GetAnnotations("MDK").ToList();
        Assert.That(annotations, Is.Empty);

        var unprotectedClassSaveMethod = unprotectedClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Save");
        annotations = unprotectedClassSaveMethod.GetAnnotations("MDK").ToList();
        Assert.That(annotations, Is.Empty);

        var unprotectedClassProgramClass = unprotectedClass.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "Program");
        annotations = unprotectedClassProgramClass.GetAnnotations("MDK").ToList();
        Assert.That(annotations, Is.Empty);

        var unprotectedClassProgramClassSaveMethod = unprotectedClassProgramClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Save");
        annotations = unprotectedClassProgramClassSaveMethod.GetAnnotations("MDK").ToList();
        Assert.That(annotations, Is.Empty);

        var unprotectedClassProgramClassMainMethod = unprotectedClassProgramClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Main");
        annotations = unprotectedClassProgramClassMainMethod.GetAnnotations("MDK").ToList();
        Assert.That(annotations, Is.Empty);

        var unprotectedClassProgramClassConstructor = unprotectedClassProgramClass.Members.OfType<ConstructorDeclarationSyntax>().First();
        annotations = unprotectedClassProgramClassConstructor.GetAnnotations("MDK").ToList();
        Assert.That(annotations, Is.Empty);
    }
}