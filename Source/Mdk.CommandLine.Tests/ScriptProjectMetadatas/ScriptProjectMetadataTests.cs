using FluentAssertions;
using Mdk.CommandLine.Commands.PackScript;
using Mdk.CommandLine.IngameScript;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.ScriptProjectMetadatas;

[TestFixture]
public class ScriptProjectMetadataTests
{
    [Test]
    public void ApplyOther_WhereThisHasOutput_AndOtherHasOutput_ExpectOtherOutput()
    {
        // Arrange
        var thisMetadata = ScriptProjectMetadata.ForOptions(
            new PackScriptParameters
            {
                MinifierLevel = MinifierLevel.None,
                TrimUnusedTypes = false,
                ProjectFile = "ThisProject.csproj",
                Output = "ThisOutput",
                Interactive = false
            },
            new Version(2, 0, 0));
        var otherMetadata = ScriptProjectMetadata.ForOptions(
            new PackScriptParameters
            {
                MinifierLevel = MinifierLevel.None,
                TrimUnusedTypes = false,
                ProjectFile = "ThisProject.csproj",
                Output = "OtherOutput",
                Interactive = false
            },
            new Version(2, 0, 0));

        // Act
        var result = thisMetadata.ApplyOther(otherMetadata);

        // Assert
        result.OutputDirectory.Should().Be("OtherOutput");
    }

    [Test]
    public void ApplyOther_WhereThisHasOutput_AndOtherHasAutoOutput_ExpectThisOutput()
    {
        // Arrange
        var thisMetadata = ScriptProjectMetadata.ForOptions(
            new PackScriptParameters
            {
                MinifierLevel = MinifierLevel.None,
                TrimUnusedTypes = false,
                ProjectFile = "ThisProject.csproj",
                Output = "ThisOutput",
                Interactive = false
            },
            new Version(2, 0, 0));
        var otherMetadata = ScriptProjectMetadata.ForOptions(
            new PackScriptParameters
            {
                MinifierLevel = MinifierLevel.None,
                TrimUnusedTypes = false,
                ProjectFile = "ThisProject.csproj",
                Output = "auto",
                Interactive = false
            },
            new Version(2, 0, 0));

        // Act
        var result = thisMetadata.ApplyOther(otherMetadata);

        // Assert
        result.OutputDirectory.Should().Be("ThisOutput");
    }

    [Test]
    public void ApplyOther_WhereThisHasNoOutput_AndOtherHasAutoOutput_ExpectAuto()
    {
        // Arrange
        var thisMetadata = ScriptProjectMetadata.ForOptions(
            new PackScriptParameters
            {
                MinifierLevel = MinifierLevel.None,
                TrimUnusedTypes = false,
                ProjectFile = "ThisProject.csproj",
                Output = null,
                Interactive = false
            },
            new Version(2, 0, 0));
        var otherMetadata = ScriptProjectMetadata.ForOptions(new PackScriptParameters
            {
                MinifierLevel = MinifierLevel.None,
                TrimUnusedTypes = false,
                ProjectFile = "ThisProject.csproj",
                Output = "auto",
                Interactive = false
            },
            new Version(2, 0, 0));

        // Act
        var result = thisMetadata.ApplyOther(otherMetadata);

        // Assert
        result.OutputDirectory.Should().Be("auto");
    }

    [Test]
    public void ApplyOther_WhereThisHasOutput_AndOtherHasNoOutput_ExpectThisOutput()
    {
        // Arrange
        var thisMetadata = ScriptProjectMetadata.ForOptions(
            new PackScriptParameters
            {
                MinifierLevel = MinifierLevel.None,
                TrimUnusedTypes = false,
                ProjectFile = "ThisProject.csproj",
                Output = "ThisOutput",
                Interactive = false
            },
            new Version(2, 0, 0));
        var otherMetadata = ScriptProjectMetadata.ForOptions(new PackScriptParameters
            {
                MinifierLevel = MinifierLevel.None,
                TrimUnusedTypes = false,
                ProjectFile = "ThisProject.csproj",
                Output = null,
                Interactive = false
            },
            new Version(2, 0, 0));

        // Act
        var result = thisMetadata.ApplyOther(otherMetadata);

        // Assert
        result.OutputDirectory.Should().Be("ThisOutput");
    }

    [Test]
    public void Close_WithAutoOutput_RunsCallback()
    {
        // Arrange
        var metadata = ScriptProjectMetadata.ForOptions(
            new PackScriptParameters
            {
                MinifierLevel = MinifierLevel.None,
                TrimUnusedTypes = false,
                ProjectFile = "ThisProject.csproj",
                Output = "auto",
                Interactive = false
            },
            new Version(2, 0, 0));

        string callback()
        {
            return "ThatOutput";
        }

        // Act
        var result = metadata.Close(callback);

        // Assert
        result.OutputDirectory.Should().Be("ThatOutput");
    }

    [Test]
    public void Close_WithOutput_DoesNotRunCallback()
    {
        // Arrange
        var metadata = ScriptProjectMetadata.ForOptions(
            new PackScriptParameters
            {
                MinifierLevel = MinifierLevel.None,
                TrimUnusedTypes = false,
                ProjectFile = "ThisProject.csproj",
                Output = "ThisOutput",
                Interactive = false
            },
            new Version(2, 0, 0));

        string callback()
        {
            return "ThatOutput";
        }

        // Act
        var result = metadata.Close(callback);

        // Assert
        result.OutputDirectory.Should().Be("ThisOutput");
    }
}