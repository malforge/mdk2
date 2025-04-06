using System.Collections.Immutable;
using FakeItEasy;
using FluentAssertions;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.Pack;
using Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;
using Mdk.CommandLine.Shared;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptPostProcessors;

[TestFixture]
public class CommentRemovalTests : DocumentProcessorTests<TypeTrimmer>
{
    [Test]
    public async Task Process_WhenGivenScriptWithComments_ShouldRemoveComments()
    {
        const string testCode =
            """
            class Program
            {
                // This is a comment
                void Main(string argument)
                {
                    // This is another comment
                    Echo(argument);
                }
                
                /* This is a block comment */
                
                /*
                 * This is a multi-line block comment
                 */ void FunctionInWeirdPlace(string argument)
                 {
                     /*
                     Echo(argument);
                     */
                 }
                 
                 void FunctionWithComment(string argument)
                 {
                     Echo(argument); // This is an inline comment
                 }
             }
            """;
        
        // const string expectedCode =
        //     """
        //     class Program
        //     {
        //         void Main(string argument)
        //         {
        //             Echo(argument);
        //         }
        //         
        //         void FunctionInWeirdPlace(string argument)
        //          {
        //              
        //          }
        //          
        //         void FunctionWithComment(string argument)
        //         {
        //             Echo(argument);
        //         }
        //     }
        //     """;
        
        // Arrange
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        var document = project.AddDocument("TestDocument", testCode);
        var processor = new TypeTrimmer();
        var parameters = new Parameters
        {
            Verb = Verb.Pack,
            PackVerb =
            {
                MinifierLevel = MinifierLevel.Trim,
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
        var result = await processor.ProcessAsync(document, context);

        // Assert
        // Write documents to string and compare them
        var expected = await document.GetTextAsync();
        var actual = await result.GetTextAsync();

        actual.ToString().Should().Be(expected.ToString());
    }
}