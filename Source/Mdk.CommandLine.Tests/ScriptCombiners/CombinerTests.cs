using FluentAssertions;
using Mdk.CommandLine.IngameScript;
using Mdk.CommandLine.IngameScript.DefaultProcessors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptCombiners;

[TestFixture]
public class CombinerTests
{
    [Test]
    public async Task CombineAsync_With5DocumentsWithVariousUsingDeclarations_CombinesAndUnifiesUsingDeclarations()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document1 = project.AddDocument("TestDocument1",
            """
            using System;
            class Program {}
            
            """);
        project = document1.Project;
        var document2 = project.AddDocument("TestDocument2",
            """
            using System;
            using System.Collections.Generic;
            class OtherClass {}
            
            """);
        project = document2.Project;
        var document3 = project.AddDocument("TestDocument3",
            """
            using System.Linq;
            class AnotherClass {}
            
            """);
        project = document3.Project;
        var document4 = project.AddDocument("TestDocument4",
            """
            using System.Text;
            using System.Collections.Generic;
            class YetAnotherClass {}
            
            """);
        project = document4.Project;
        var document5 = project.AddDocument("TestDocument5",
            """
            using System.Threading.Tasks;
            class AndAnotherClass {}
            
            """);
        project = document5.Project;
        var combiner = new Combiner();
        var metadata = new ScriptProjectMetadata
        {
            MdkProjectVersion = new Version(2, 0, 0),
            ProjectDirectory = @"A:\Fake\Path",
            OutputDirectory = @"A:\Fake\Path\Output",
            Macros = new Dictionary<string, string>()
        };

        // Act
        var result = await combiner.CombineAsync(project, new[] { document1, document2, document3, document4, document5 }, metadata);

        // Assert
        result.Should().NotBeNull();
        var syntaxTree = await result.GetSyntaxTreeAsync();
        syntaxTree.Should().NotBeNull();
        var root = await syntaxTree!.GetRootAsync();
        root.Should().NotBeNull();
        var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>().Select(u => u.Name?.ToString()).ToList();
        usingDirectives.Should().HaveCount(5);
        usingDirectives.Should().BeEquivalentTo("System", "System.Collections.Generic", "System.Linq", "System.Text", "System.Threading.Tasks");
    }
}