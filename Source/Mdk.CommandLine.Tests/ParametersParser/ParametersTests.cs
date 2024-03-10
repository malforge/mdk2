using FluentAssertions;
using Mdk.CommandLine;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ParametersParser;

[TestFixture]
public class ParametersTests
{
    [Test]
    public void Help_WithAllTypes_ShouldWriteHelp()
    {
        var parameters = Parameters.Create("Test", "test", new SemanticVersion(1, 2, 3))
            .WithExtraHelp("This is a test program.")
            .WithPreferredBoolStyle(Parameters.PreferredBoolStyle.YesNo)
            .WithArgument("input", "The input file.")
            .WithArgument("output", "The output file.")
            .WithOptionalArgument("optional", "An optional argument.")
            .WithSwitch("verbose", "Enable verbose output.")
            .WithOption("log", "The log file.", "log.txt");
        var writer = new StringWriter();
        parameters.Help(writer);
        Console.WriteLine(writer);
//         writer.ToString().ReplaceLineEndings("\n").Should().Be(@"test - This is a test program.
    }
}