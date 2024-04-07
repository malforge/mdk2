using System.Collections.Immutable;
using FakeItEasy;
using FluentAssertions;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;
using Mdk.CommandLine.SharedApi;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptPostProcessors;

[TestFixture]
public class SymbolProtectionAnnotatorTests : ScriptPostProcessorTests<RegionAnnotator>
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
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        // Act
        var result = processor.ProcessAsync(document, context).Result;

        // Assert
        result.Should().NotBeSameAs(document);
        var root = await result.GetSyntaxRootAsync();
        root.Should().NotBeNull();

        var programClass = root!.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "Program");
        var annotations = programClass.GetAnnotations("MDKProtectedSymbol");
        annotations.Should().NotBeEmpty();

        var saveMethods = programClass.Members.OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.Text == "Save").ToList();
        saveMethods.Should().HaveCount(3);
        foreach (var saveMethod in saveMethods)
        {
            annotations = saveMethod.GetAnnotations("MDKProtectedSymbol");
            annotations.Should().NotBeEmpty();
        }

        var mainMethods = programClass.Members.OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.Text == "Main").ToList();
        mainMethods.Should().HaveCount(4);
        foreach (var mainMethod in mainMethods)
        {
            annotations = mainMethod.GetAnnotations("MDKProtectedSymbol");
            annotations.Should().NotBeEmpty();
        }

        var unprotectedMethod = programClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "UnprotectedMethod");
        annotations = unprotectedMethod.GetAnnotations("MDKProtectedSymbol");
        annotations.Should().BeEmpty();

        var unprotectedProperty = programClass.Members.OfType<PropertyDeclarationSyntax>().First(p => p.Identifier.Text == "UnprotectedProperty");
        annotations = unprotectedProperty.GetAnnotations("MDKProtectedSymbol");
        annotations.Should().BeEmpty();

        var unprotectedField = programClass.Members.OfType<FieldDeclarationSyntax>().First(f => f.Declaration.Variables.First().Identifier.Text == "_unprotectedField");
        annotations = unprotectedField.GetAnnotations("MDKProtectedSymbol");
        annotations.Should().BeEmpty();

        var unprotectedNestedClass = programClass.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "UnprotectedNestedClass");
        annotations = unprotectedNestedClass.GetAnnotations("MDKProtectedSymbol");
        annotations.Should().BeEmpty();

        var unprotectedNestedSaveMethod = unprotectedNestedClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Save");
        annotations = unprotectedNestedSaveMethod.GetAnnotations("MDKProtectedSymbol");
        annotations.Should().BeEmpty();

        var unprotectedNestedMainMethod = unprotectedNestedClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Main");
        annotations = unprotectedNestedMainMethod.GetAnnotations("MDKProtectedSymbol");
        annotations.Should().BeEmpty();

        var unprotectedClass = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "UnprotectedClass");
        annotations = unprotectedClass.GetAnnotations("MDKProtectedSymbol");
        annotations.Should().BeEmpty();

        var unprotectedClassMainMethod = unprotectedClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Main");
        annotations = unprotectedClassMainMethod.GetAnnotations("MDKProtectedSymbol");
        annotations.Should().BeEmpty();

        var unprotectedClassSaveMethod = unprotectedClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Save");
        annotations = unprotectedClassSaveMethod.GetAnnotations("MDKProtectedSymbol");
        annotations.Should().BeEmpty();

        var unprotectedClassProgramClass = unprotectedClass.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "Program");
        annotations = unprotectedClassProgramClass.GetAnnotations("MDKProtectedSymbol");
        annotations.Should().BeEmpty();

        var unprotectedClassProgramClassSaveMethod = unprotectedClassProgramClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Save");
        annotations = unprotectedClassProgramClassSaveMethod.GetAnnotations("MDKProtectedSymbol");
        annotations.Should().BeEmpty();

        var unprotectedClassProgramClassMainMethod = unprotectedClassProgramClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Main");
        annotations = unprotectedClassProgramClassMainMethod.GetAnnotations("MDKProtectedSymbol");
        annotations.Should().BeEmpty();

        var unprotectedClassProgramClassConstructor = unprotectedClassProgramClass.Members.OfType<ConstructorDeclarationSyntax>().First();
        annotations = unprotectedClassProgramClassConstructor.GetAnnotations("MDKProtectedSymbol");
        annotations.Should().BeEmpty();
    }
}