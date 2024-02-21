using FluentAssertions;
using Mdk.CommandLine.IngameScript;
using Mdk.CommandLine.IngameScript.DefaultProcessors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptPostProcessors;

[TestFixture]
public class AnnotatorTests : ScriptPostProcessorTests<Annotator>
{
    [Test]
    public void ProcessAsync_WithoutAnyAnnotations_ReturnsDocument()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", "class Program {}");
        var processor = new Annotator();
        var metadata = new ScriptProjectMetadata
        {
            MdkProjectVersion = new Version(2, 0, 0),
            ProjectDirectory = @"A:\Fake\Path",
            OutputDirectory = @"A:\Fake\Path\Output",
            Macros = new Dictionary<string, string>()
        };

        // Act
        var result = processor.ProcessAsync(document, metadata).Result;

        // Assert
        result.Should().BeSameAs(document);
    }

    [Test]
    public async Task ProcessAsync_WithMDKRegions_ReturnsAnnotatedDocument()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument",
            """
            class UnAnnotatedClass
            {
            }

            #region mdk preserve
            class AnnotatedClass
            {
            }
            #endregion

            class ClassWithInternalRegion
            {
            #region mdk preserve
            public string AnnotatedProperty { get; set; }
            #endregion
            }
            """);
        var processor = new Annotator();
        var metadata = new ScriptProjectMetadata
        {
            MdkProjectVersion = new Version(2, 0, 0),
            ProjectDirectory = @"A:\Fake\Path",
            OutputDirectory = @"A:\Fake\Path\Output",
            Macros = new Dictionary<string, string>()
        };

        // Act
        var result = processor.ProcessAsync(document, metadata).Result;

        // Assert
        result.Should().NotBeSameAs(document);
        var root = await result.GetSyntaxRootAsync();
        root.Should().NotBeNull();

        var unAnnotatedClass = root!.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "UnAnnotatedClass");
        var annotations = unAnnotatedClass.GetAnnotations("mdk");
        annotations.Should().BeEmpty();

        var annotatedClass = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "AnnotatedClass");
        annotations = annotatedClass.GetAnnotations("mdk");
        annotations.Should().NotBeEmpty();

        var classWithInternalRegion = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "ClassWithInternalRegion");
        annotations = classWithInternalRegion.GetAnnotations("mdk");
        annotations.Should().BeEmpty();
        var annotatedProperty = classWithInternalRegion.DescendantNodes().OfType<PropertyDeclarationSyntax>().First(p => p.Identifier.Text == "AnnotatedProperty");
        annotations = annotatedProperty.GetAnnotations("mdk");
        annotations.Should().NotBeEmpty();
    }

    [Test]
    public async Task ProcessAsync_WithNoRegions_StillAnnotateProtectedIdentifiers()
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
                
                public void Main(string argument, UpdateType updateSource)
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
        
        var processor = new Annotator();
        var metadata = new ScriptProjectMetadata
        {
            MdkProjectVersion = new Version(2, 0, 0),
            ProjectDirectory = @"A:\Fake\Path",
            OutputDirectory = @"A:\Fake\Path\Output",
            Macros = new Dictionary<string, string>()
        };
        
        // Act
        var result = processor.ProcessAsync(document, metadata).Result;
        
        // Assert
        result.Should().NotBeSameAs(document);
        var root = await result.GetSyntaxRootAsync();
        root.Should().NotBeNull();
        
        var programClass = root!.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "Program");
        var annotations = programClass.GetAnnotations("mdk");
        annotations.Should().NotBeEmpty();
        
        var saveMethod = programClass.DescendantNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Save");
        annotations = saveMethod.GetAnnotations("mdk");
        annotations.Should().NotBeEmpty();
        
        var mainMethod = programClass.DescendantNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Main");
        annotations = mainMethod.GetAnnotations("mdk");
        annotations.Should().NotBeEmpty();
        
        var unprotectedMethod = programClass.DescendantNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "UnprotectedMethod");
        annotations = unprotectedMethod.GetAnnotations("mdk");
        annotations.Should().BeEmpty();
        
        var unprotectedProperty = programClass.DescendantNodes().OfType<PropertyDeclarationSyntax>().First(p => p.Identifier.Text == "UnprotectedProperty");
        annotations = unprotectedProperty.GetAnnotations("mdk");
        annotations.Should().BeEmpty();
        
        var unprotectedField = programClass.DescendantNodes().OfType<FieldDeclarationSyntax>().First(f => f.Declaration.Variables.First().Identifier.Text == "_unprotectedField");
        annotations = unprotectedField.GetAnnotations("mdk");
        annotations.Should().BeEmpty();
        
        var unprotectedNestedClass = programClass.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "UnprotectedNestedClass");
        annotations = unprotectedNestedClass.GetAnnotations("mdk");
        annotations.Should().BeEmpty();

        var unprotectedNestedSaveMethod = unprotectedNestedClass.DescendantNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Save");
        annotations = unprotectedNestedSaveMethod.GetAnnotations("mdk");
        annotations.Should().BeEmpty();
        
        var unprotectedNestedMainMethod = unprotectedNestedClass.DescendantNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Main");
        annotations = unprotectedNestedMainMethod.GetAnnotations("mdk");
        annotations.Should().BeEmpty();
        
        var unprotectedClass = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "UnprotectedClass");
        annotations = unprotectedClass.GetAnnotations("mdk");
        annotations.Should().BeEmpty();
        
        var unprotectedClassMainMethod = unprotectedClass.DescendantNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Main");
        annotations = unprotectedClassMainMethod.GetAnnotations("mdk");
        annotations.Should().BeEmpty();
        
        var unprotectedClassSaveMethod = unprotectedClass.DescendantNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Save");
        annotations = unprotectedClassSaveMethod.GetAnnotations("mdk");
        annotations.Should().BeEmpty();
        
        var unprotectedClassProgramClass = unprotectedClass.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "Program");
        annotations = unprotectedClassProgramClass.GetAnnotations("mdk");
        annotations.Should().BeEmpty();
        
        var unprotectedClassProgramClassSaveMethod = unprotectedClassProgramClass.DescendantNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Save");
        annotations = unprotectedClassProgramClassSaveMethod.GetAnnotations("mdk");
        annotations.Should().BeEmpty();
        
        var unprotectedClassProgramClassMainMethod = unprotectedClassProgramClass.DescendantNodes().OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "Main");
        annotations = unprotectedClassProgramClassMainMethod.GetAnnotations("mdk");
        annotations.Should().BeEmpty();
        
        var unprotectedClassProgramClassConstructor = unprotectedClassProgramClass.DescendantNodes().OfType<ConstructorDeclarationSyntax>().First();
        annotations = unprotectedClassProgramClassConstructor.GetAnnotations("mdk");
        annotations.Should().BeEmpty();
    }
}