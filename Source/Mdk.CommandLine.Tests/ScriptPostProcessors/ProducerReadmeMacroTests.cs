using System.Collections.Immutable;
using System.Text;
using FakeItEasy;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;
using Mdk.CommandLine.Shared;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptPostProcessors;

/// <summary>
/// Tests for macro replacement in Instructions.readme file via Producer.
/// </summary>
[TestFixture]
public class ProducerReadmeMacroTests
{
    [Test]
    public async Task ProduceAsync_ReplacesDefaultMacrosInReadme()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("Program.cs",
            """
            namespace IngameScript
            {
                public class Program : MyGridProgram
                {
                    public void Main(string argument) { }
                }
            }
            """);

        var readmeText = """
            This script was built on $MDK_DATETIME$
            Date: $MDK_DATE$
            Time: $MDK_TIME$
            """;
        var readme = project.AddAdditionalDocument("Instructions.readme", readmeText);

        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb = 
            {
                MinifierLevel = MinifierLevel.None,
                ProjectFile = @"A:\Fake\Path\Project.csproj",
                Output = @"A:\Fake\Path\Output",
                DryRun = true,  // Don't write to disk
                Macros =
                {
                    ["$MDK_DATETIME$"] = "2024-01-01 12:00:00",
                    ["$MDK_DATE$"] = "2024-01-01",
                    ["$MDK_TIME$"] = "12:00:00"
                }
            }
        };

        var context = new PackContext(
            parameters,
            A.Fake<IConsole>(),
            A.Fake<IInteraction>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        var producer = new Producer();
        var script = new StringBuilder("public class Program { }");
        var outputDir = new DirectoryInfo(@"A:\Fake\Output");

        // Act
        var result = await producer.ProduceAsync(outputDir, script, readme, null, context);

        // Assert
        Assert.That(result, Is.Not.Empty, "Should produce files");
        var scriptFile = result.FirstOrDefault(f => f.Id == "script.cs");
        Assert.That(scriptFile, Is.Not.Null, "Should produce script.cs");

        var content = scriptFile!.Content;

        // Macros should be replaced
        Assert.That(content, Does.Not.Contain("$MDK_DATETIME$"), 
            "MDK_DATETIME macro should be replaced");
        Assert.That(content, Does.Not.Contain("$MDK_DATE$"), 
            "MDK_DATE macro should be replaced");
        Assert.That(content, Does.Not.Contain("$MDK_TIME$"), 
            "MDK_TIME macro should be replaced");

        // Replaced values should be present
        Assert.That(content, Does.Contain("2024-01-01 12:00:00"), 
            "MDK_DATETIME value should be in output");
        Assert.That(content, Does.Contain("2024-01-01"), 
            "MDK_DATE value should be in output");

        // Readme should be present as comments
        Assert.That(content, Does.Contain("// This script was built on"), 
            "Readme should be in output as comments");
    }

    [Test]
    public async Task ProduceAsync_ReplacesCustomMacrosInReadme()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("Program.cs", "public class Program { }");

        var readmeText = """
            Version: $CUSTOM_VERSION$
            Author: $AUTHOR_NAME$
            """;
        var readme = project.AddAdditionalDocument("Instructions.readme", readmeText);

        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb = 
            {
                MinifierLevel = MinifierLevel.None,
                ProjectFile = @"A:\Fake\Path\Project.csproj",
                Output = @"A:\Fake\Path\Output",
                DryRun = true,  // Don't write to disk
                Macros =
                {
                    ["$CUSTOM_VERSION$"] = "1.2.3",
                    ["$AUTHOR_NAME$"] = "TestUser"
                }
            }
        };

        var context = new PackContext(
            parameters,
            A.Fake<IConsole>(),
            A.Fake<IInteraction>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        var producer = new Producer();
        var script = new StringBuilder("public class Program { }");
        var outputDir = new DirectoryInfo(@"A:\Fake\Output");

        // Act
        var result = await producer.ProduceAsync(outputDir, script, readme, null, context);

        // Assert
        var scriptFile = result.FirstOrDefault(f => f.Id == "script.cs");
        Assert.That(scriptFile, Is.Not.Null);

        var content = scriptFile!.Content;

        Assert.That(content, Does.Not.Contain("$CUSTOM_VERSION$"), 
            "CUSTOM_VERSION macro should be replaced");
        Assert.That(content, Does.Not.Contain("$AUTHOR_NAME$"), 
            "AUTHOR_NAME macro should be replaced");

        Assert.That(content, Does.Contain("// Version: 1.2.3"), 
            "Custom version should be in output");
        Assert.That(content, Does.Contain("// Author: TestUser"), 
            "Custom author should be in output");
    }

    [Test]
    public async Task ProduceAsync_LeavesUnknownMacrosUnchangedInReadme()
    {
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("Program.cs", "public class Program { }");

        var readmeText = """
            Known: $MDK_DATE$
            Unknown: $UNKNOWN_MACRO$
            """;
        var readme = project.AddAdditionalDocument("Instructions.readme", readmeText);

        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb = 
            {
                MinifierLevel = MinifierLevel.None,
                ProjectFile = @"A:\Fake\Path\Project.csproj",
                Output = @"A:\Fake\Path\Output",
                DryRun = true,  // Don't write to disk
                Macros =
                {
                    ["$MDK_DATE$"] = "2024-01-01"
                }
            }
        };

        var context = new PackContext(
            parameters,
            A.Fake<IConsole>(),
            A.Fake<IInteraction>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileFilter>(o => o.Strict()),
            A.Fake<IFileSystem>(),
            A.Fake<IImmutableSet<string>>(o => o.Strict())
        );

        var producer = new Producer();
        var script = new StringBuilder("public class Program { }");
        var outputDir = new DirectoryInfo(@"A:\Fake\Output");

        // Act
        var result = await producer.ProduceAsync(outputDir, script, readme, null, context);

        // Assert
        var scriptFile = result.FirstOrDefault(f => f.Id == "script.cs");
        Assert.That(scriptFile, Is.Not.Null);

        var content = scriptFile!.Content;

        // Known macro should be replaced
        Assert.That(content, Does.Not.Contain("$MDK_DATE$"), 
            "Known macro should be replaced");
        Assert.That(content, Does.Contain("2024-01-01"), 
            "Known macro value should be present");

        // Unknown macro should be left unchanged
        Assert.That(content, Does.Contain("$UNKNOWN_MACRO$"), 
            "Unknown macro should be left unchanged");
    }
}
