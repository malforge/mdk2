using Mdk.CommandLine.Utility;
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
        Assert.That(result, Is.True);
        Assert.That(parsed["mdk2"].Keys, Is.Not.Empty);
        Assert.That(parsed["mdk2"]["minifier"].ToString(), Is.EqualTo("advanced"));
        Assert.That(parsed["mdk2"]["trimUnusedTypes"].ToBool(), Is.True);
        Assert.That(parsed["mdk2"]["output"].ToString(), Is.EqualTo("bin"));
        Assert.That(parsed["mdk2"]["interactive"].ToBool(), Is.False);

        Console.WriteLine(parsed);
    }

    [Test]
    public void TryParse_ForSectionWithLeadingComment_ReturnsTrue()
    {
        var ini = """

                  ; This is a comment
                  [a-section]
                  key=value

                  """;
        var result = Ini.TryParse(ini, out var parsed);
        Assert.That(result, Is.True);
        Assert.That(parsed["a-section"].Keys, Is.Not.Empty);
        Assert.That(parsed["a-section"]["key"].ToString(), Is.EqualTo("value"));
        Assert.That(parsed["a-section"].LeadingComment!.Replace("\r", ""), Is.EqualTo(
            """
            
            ; This is a comment
            """.Replace("\r", "")));

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
        Assert.That(result, Is.True);
        Assert.That(parsed["non-existing-section"].Keys, Is.Empty);
        Assert.That(parsed["a-section"]["non-existing-key"].ToString("default"), Is.EqualTo("default"));
        Assert.That(parsed["non-existing-section"]["non-existing-key"].ToString("default"), Is.EqualTo("default"));
    }

    [Test]
    public void Indexer_ForEnumKey_ReturnsEnumValue()
    {
        var ini = @"
[a-section]
key=EnumValue
";
        var result = Ini.TryParse(ini, out var parsed);
        Assert.That(result, Is.True);
        Assert.That(parsed["a-section"]["key"].ToEnum<EnumType>(), Is.EqualTo(EnumType.EnumValue));
    }

    [Test]
    public void Indexer_ForEnumKeyWithInvalidValue_ReturnsDefault()
    {
        var ini = @"
[a-section]
key=InvalidValue
";
        var result = Ini.TryParse(ini, out var parsed);
        Assert.That(result, Is.True);
        Assert.That(parsed["a-section"]["key"].ToEnum(EnumType.Default), Is.EqualTo(EnumType.Default));
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
        Assert.That(ini.ToString().Replace("\r", ""), Is.EqualTo(expected.Replace("\r", "")));
    }

    enum EnumType
    {
        None,
        EnumValue,
        Default
    }
}