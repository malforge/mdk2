using System.Collections.Immutable;
using FakeItEasy;
using FluentAssertions;
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
        result.Should().NotBeSameAs(document);
        var root = await result.GetSyntaxRootAsync();
        root.Should().NotBeNull();

        var programClass = root!.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "Program");
        var annotations = programClass.GetAnnotations("MDK").ToList();
        annotations.Should().NotBeEmpty();
        annotations.Count.Should().Be(1);
        annotations[0].Data.Should().Be("preserve");

        var saveMethods = programClass.Members.OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.Text == "Save").ToList();
        saveMethods.Should().HaveCount(3);
        foreach (var saveMethod in saveMethods)
        {
            annotations = saveMethod.GetAnnotations("MDK").ToList();
            annotations.Should().NotBeEmpty();
            annotations.Count.Should().Be(1);
            annotations[0].Data.Should().Be("preserve");
        }

        var mainMethods = programClass.Members.OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.Text == "Main").ToList();
        mainMethods.Should().HaveCount(4);
        foreach (var mainMethod in mainMethods)
        {
            annotations = mainMethod.GetAnnotations("MDK").ToList();
            annotations.Should().NotBeEmpty();
            annotations.Count.Should().Be(1);
            annotations[0].Data.Should().Be("preserve");
            annotations.Should().NotBeEmpty();
        }

        var unprotectedMethod = programClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "UnprotectedMethod");
        annotations = unprotectedMethod.GetAnnotations("MDK").ToList();
        annotations.Should().BeEmpty();

        var unprotectedProperty = programClass.Members.OfType<PropertyDeclarationSyntax>().First(p => p.Identifier.Text == "UnprotectedProperty");
        annotations = unprotectedProperty.GetAnnotations("MDK").ToList();
        annotations.Should().BeEmpty();

        var unprotectedField = programClass.Members.OfType<FieldDeclarationSyntax>().First(f => f.Declaration.Variables.First().Identifier.Text == "_unprotectedField");
        annotations = unprotectedField.GetAnnotations("MDK").ToList();
        annotations.Should().BeEmpty();

        var unprotectedNestedClass = programClass.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "UnprotectedNestedClass");
        annotations = unprotectedNestedClass.GetAnnotations("MDK").ToList();
        annotations.Should().BeEmpty();

        var unprotectedNestedSaveMethod = unprotectedNestedClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Save");
        annotations = unprotectedNestedSaveMethod.GetAnnotations("MDK").ToList();
        annotations.Should().BeEmpty();

        var unprotectedNestedMainMethod = unprotectedNestedClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Main");
        annotations = unprotectedNestedMainMethod.GetAnnotations("MDK").ToList();
        annotations.Should().BeEmpty();

        var unprotectedClass = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "UnprotectedClass");
        annotations = unprotectedClass.GetAnnotations("MDK").ToList();
        annotations.Should().BeEmpty();

        var unprotectedClassMainMethod = unprotectedClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Main");
        annotations = unprotectedClassMainMethod.GetAnnotations("MDK").ToList();
        annotations.Should().BeEmpty();

        var unprotectedClassSaveMethod = unprotectedClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Save");
        annotations = unprotectedClassSaveMethod.GetAnnotations("MDK").ToList();
        annotations.Should().BeEmpty();

        var unprotectedClassProgramClass = unprotectedClass.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "Program");
        annotations = unprotectedClassProgramClass.GetAnnotations("MDK").ToList();
        annotations.Should().BeEmpty();

        var unprotectedClassProgramClassSaveMethod = unprotectedClassProgramClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Save");
        annotations = unprotectedClassProgramClassSaveMethod.GetAnnotations("MDK").ToList();
        annotations.Should().BeEmpty();

        var unprotectedClassProgramClassMainMethod = unprotectedClassProgramClass.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Main");
        annotations = unprotectedClassProgramClassMainMethod.GetAnnotations("MDK").ToList();
        annotations.Should().BeEmpty();

        var unprotectedClassProgramClassConstructor = unprotectedClassProgramClass.Members.OfType<ConstructorDeclarationSyntax>().First();
        annotations = unprotectedClassProgramClassConstructor.GetAnnotations("MDK").ToList();
        annotations.Should().BeEmpty();
    }
}