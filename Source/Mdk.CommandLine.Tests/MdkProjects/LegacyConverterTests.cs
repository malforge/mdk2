using System.Net;
using System.Text;
using System.Xml.Linq;
using FakeItEasy;
using FluentAssertions;
using Mdk.CommandLine;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.IngameScript.LegacyConversion;
using Mdk.CommandLine.SharedApi;
using NUnit.Framework;
using Mdk.CommandLine.Utility;

namespace MDK.CommandLine.Tests.MdkProjects;

[TestFixture]
public class LegacyConverterTests
{
    static readonly XNamespace MsbuildNs = "http://schemas.microsoft.com/developer/msbuild/2003";

    [Test]
    public async Task ConvertAsync_WithValidOldProject_ConvertsProject()
    {
        // Arrange
        var console = A.Fake<IConsole>();
        var httpClient = A.Fake<IHttpClient>();
        A.CallTo(() => httpClient.GetAsync(A<string>._, A<TimeSpan>._)).Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent( 
                """
                {
                  "versions": [
                    "2.0.0-alpha001",
                    "2.0.0-alpha027",
                    "2.0.0-alpha028",
                    "2.0.0-alpha030",
                    "2.0.0-alpha031",
                    "2.0.0-alpha032"
                  ]
                }
                """, Encoding.UTF8, "application/json")
        }));
        
        var projectDirectory = Path.Combine(Path.GetTempPath(), "MDK", "LegacyConverterTests", Guid.NewGuid().ToString());
        var projectFileName = Path.Combine(projectDirectory, "LegacyScriptProject.csproj");
        try
        {
            CreateLocalProjectCopy(projectDirectory);

            var mdkProject = await MdkProject.LoadAsync(projectFileName, console).SingleAsync();
            mdkProject.Type.Should().Be(MdkProjectType.LegacyProgrammableBlock);
            mdkProject.Project.Should().NotBeNull();
            
            var converter = new LegacyConverter();
            var parameters = new Parameters()
            {
                Verb = Verb.Restore,
                RestoreVerb =
                {
                    ProjectFile = projectFileName
                }
            };

            // Act
            await converter.ConvertAsync(parameters, mdkProject, console, httpClient);
            var projectFileContent = await File.ReadAllTextAsync(projectFileName);
            Console.WriteLine($"--- {projectFileName} ---");
            Console.WriteLine(projectFileContent);
            
            // Assert
            var document = XDocument.Parse(projectFileContent);

            var mainIniFileName = Path.Combine(projectDirectory, "LegacyScriptProject.mdk.ini");
            File.Exists(mainIniFileName).Should().BeTrue();
            var iniContent = await File.ReadAllTextAsync(mainIniFileName);
            Console.WriteLine($"--- {mainIniFileName} ---");
            Console.WriteLine(iniContent);
            
            var localIniFileName = Path.Combine(projectDirectory, "LegacyScriptProject.mdk.local.ini");
            File.Exists(localIniFileName).Should().BeTrue();
            iniContent = await File.ReadAllTextAsync(localIniFileName);
            Console.WriteLine($"--- {localIniFileName} ---");
            Console.WriteLine(iniContent);
            
            var gitIgnoreFileName = Path.Combine(projectDirectory, ".gitignore");
            File.Exists(gitIgnoreFileName).Should().BeTrue();
            var gitIgnoreContent = await File.ReadAllTextAsync(gitIgnoreFileName);
            Console.WriteLine($"--- {gitIgnoreFileName} ---");
            Console.WriteLine(gitIgnoreContent);
            
            // // document should not contain any reference to the legacy props files
            // // - as imports:
            // document.Elements(MsbuildNs, "Project", "Import")
            //     .WithAttribute("Project", a => ((string?)a)?.EndsWith("mdk.options.props", StringComparison.OrdinalIgnoreCase) == true)
            //     .Should().BeEmpty();
            // document.Elements(MsbuildNs, "Project", "Import")
            //     .WithAttribute("Project", a => ((string?)a)?.EndsWith("mdk.paths.props", StringComparison.OrdinalIgnoreCase) == true)
            //     .Should().BeEmpty();
            // // - as additional files:
            // document.Elements(MsbuildNs, "Project", "ItemGroup", "AdditionalFiles")
            //     .WithAttribute("Include", a => ((string?)a)?.EndsWith("mdk.options.props", StringComparison.OrdinalIgnoreCase) == true)
            //     .Should().BeEmpty();
            // document.Elements(MsbuildNs, "Project", "ItemGroup", "AdditionalFiles")
            //     .WithAttribute("Include", a => ((string?)a)?.EndsWith("mdk.paths.props", StringComparison.OrdinalIgnoreCase) == true)
            //     .Should().BeEmpty();
            // // - as copy commands:
            // document.Elements(MsbuildNs, "Project", "Target", "Copy")
            //     .WithAttribute("SourceFiles", a => ((string?)a)?.EndsWith("mdk.options.props", StringComparison.OrdinalIgnoreCase) == true).Should().BeEmpty();
            // document.Elements(MsbuildNs, "Project", "Target", "Copy")
            //     .WithAttribute("SourceFiles", a => ((string?)a)?.EndsWith("mdk.paths.props", StringComparison.OrdinalIgnoreCase) == true).Should().BeEmpty();
            
            // document should contain a reference to the Mal.Mdk2.PbPackager package
            document.Elements(MsbuildNs, "Project", "ItemGroup", "PackageReference").WithAttribute("Include", "Mal.Mdk2.PbPackager").Should().HaveCount(1);
            
            // // document should contain a reference to the Mal.Mdk2.References package
            // document.Elements(MsbuildNs, "Project", "ItemGroup", "PackageReference").WithAttribute("Include", "Mal.Mdk2.References").Should().HaveCount(1);
            //
            // // document should contain a reference to the Mal.Mdk2.PbAnalyzer package
            // document.Elements(MsbuildNs, "Project", "ItemGroup", "PackageReference").WithAttribute("Include", "Mal.Mdk2.PbAnalyzer").Should().HaveCount(1);
        }
        finally
        {
            if (Directory.Exists(projectDirectory))
                Directory.Delete(projectDirectory, true);
        }
    }

    static void CreateLocalProjectCopy(string projectDirectory)
    {
        Directory.CreateDirectory(projectDirectory);
        var sourceDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "LegacyScriptProject");
        foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var destinationFile = Path.Combine(projectDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            File.Copy(sourceFile, destinationFile);
        }
    }
}