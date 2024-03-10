using FluentAssertions;
using Mdk.CommandLine;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.IniParser;

[TestFixture]
public class IniTests
{
    [Test]
    public void TryParse_WhenGivenValidIniString_ReturnsTrue()
    {
        var ini = @"
[mdk2]
minifier=advanced
trimUnusedTypes=true
output=bin
interactive=false
";
        var result = Ini.TryParse(ini, out var parsed);
        result.Should().BeTrue();
        parsed["mdk2"].Keys.Should().NotBeEmpty();
        parsed["mdk2"]["minifier"].ToString().Should().Be("advanced");
        parsed["mdk2"]["trimUnusedTypes"].ToBool().Should().BeTrue();
        parsed["mdk2"]["output"].ToString().Should().Be("bin");
        parsed["mdk2"]["interactive"].ToBool().Should().BeFalse();

        Console.WriteLine(parsed);
    }

    [Test]
    public void TryParse_ForSectionWithLeadingComment_ReturnsTrue()
    {
        var ini = @"
; This is a comment
[a-section]
key=value
";
        var result = Ini.TryParse(ini, out var parsed);
        result.Should().BeTrue();
        parsed["a-section"].Keys.Should().NotBeEmpty();
        parsed["a-section"]["key"].ToString().Should().Be("value");
        parsed["a-section"].LeadingComment!.Replace("\r", "").Should().Be(@"
; This is a comment".Replace("\r", ""));

        Console.WriteLine(parsed);
    }

    [Test]
    public void Indexer_ForNonExistingSectionAndKey_ReturnsDefaults()
    {
        var ini = @"
[a-section]
key=value
";
        var result = Ini.TryParse(ini, out var parsed);
        result.Should().BeTrue();
        parsed["non-existing-section"].Keys.Should().BeEmpty();
        parsed["a-section"]["non-existing-key"].ToString("default").Should().Be("default");
        parsed["non-existing-section"]["non-existing-key"].ToString("default").Should().Be("default");
    }

    [Test]
    public void Indexer_ForEnumKey_ReturnsEnumValue()
    {
        var ini = @"
[a-section]
key=EnumValue
";
        var result = Ini.TryParse(ini, out var parsed);
        result.Should().BeTrue();
        parsed["a-section"]["key"].ToEnum<EnumType>().Should().Be(EnumType.EnumValue);
    }

    [Test]
    public void Indexer_ForEnumKeyWithInvalidValue_ReturnsDefault()
    {
        var ini = @"
[a-section]
key=InvalidValue
";
        var result = Ini.TryParse(ini, out var parsed);
        result.Should().BeTrue();
        parsed["a-section"]["key"].ToEnum(EnumType.Default).Should().Be(EnumType.Default);
    }

    [Test]
    [Ignore("Apparently, the order of the keys is not guaranteed")]
    public void Ini_ConstructingNewIniWithSectionsAndKeys_ReturnsExpectedIniString()
    {
        var ini = new Ini()
            .WithSection("a-section")
            .WithKey("a-section", "key", "value")
            .WithKey("a-section", "enum-key", EnumType.EnumValue)
            .WithKey("a-section", "bool-key", true)
            .WithKey("a-section", "int-key", 42)
            .WithKey("a-section", "float-key", 3.14f)
            .WithKey("a-section", "double-key", 3.14)
            .WithKey("a-section", "decimal-key", 3.14m)
            .WithKey("a-section", "long-key", 42L)
            .WithKey("a-section", "short-key", (short)42)
            .WithKey("a-section", "byte-key", (byte)42)
            .WithKey("a-section", "sbyte-key", (sbyte)42)
            .WithKey("a-section", "char-key", 'c');

        const string expected =
            """
            [a-section]
            key=value
            decimal-key=3.14
            enum-key=EnumValue
            char-key=c
            double-key=3.14
            float-key=3.14
            byte-key=42
            int-key=42
            short-key=42
            bool-key=true
            long-key=42
            sbyte-key=42
            
            """;
        ini.ToString().Replace("\r", "").Should().Be(expected.Replace("\r", ""));
    }

    enum EnumType
    {
        None,
        EnumValue,
        Default
    }
}