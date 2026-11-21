using System.Text;
using Mdk.CommandLine;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.Utilities;

[TestFixture]
public class StringBuilderExtensionsTests
{
    [TestCase("\tx", "    x")]
    [TestCase("\t\tx", "        x")]
    [TestCase("ab\tx", "ab  x")]
    [TestCase("abcd\tx", "abcd    x")]
    [TestCase("a\nb\tx", "a\nb   x")]
    [TestCase("a\r\nb\tx", "a\r\nb   x")]
    [TestCase("abc\n\tx", "abc\n    x")]
    public void ConvertTabsToSpaces_AlignsToTabStops(string input, string expected)
    {
        // Arrange
        var buffer = new StringBuilder(input);

        // Act
        buffer.ConvertTabsToSpaces(4);

        // Assert
        Assert.That(buffer.ToString(), Is.EqualTo(expected));
    }
}
