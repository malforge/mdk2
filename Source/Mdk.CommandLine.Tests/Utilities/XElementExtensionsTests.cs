using System.Xml.Linq;
using FluentAssertions;
using Mdk.CommandLine.Utility;
using NUnit.Framework;

namespace MDK.CommandLine.Tests.Utilities;

[TestFixture]
public class XElementExtensionsTests
{
    [Test]
    public void Element_WhereElementExists_ReturnsElement()
    {
        // Arrange
        var xml = "<root><child><grandchild /></child></root>";
        var element = XElement.Parse(xml);

        // Act
        var result = element.Element("child", "grandchild");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(XName.Get("grandchild"));
    }

    [Test]
    public void Elements_WhereElementsExist_ReturnsElements()
    {
        // Arrange
        var xml = "<root><child><grandchild /></child><child><grandchild /></child></root>";
        var element = XElement.Parse(xml);

        // Act
        var result = element.Elements("child", "grandchild").ToList();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(x => x.Name == XName.Get("grandchild"));
    }

    [Test]
    public void Element_WhereElementExistsInAnotherChildInstance_ReturnsElement()
    {
        // Arrange
        var xml = "<root><child></child><child><grandchild /></child></root>";
        var element = XElement.Parse(xml);

        // Act
        var result = element.Element("child", "grandchild");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(XName.Get("grandchild"));
    }
    
    [Test]
    public void Elements_WhereElementsExistsInAnotherChildInstance_ReturnsElements()
    {
        // Arrange
        var xml = "<root><child></child><child><grandchild /></child></root>";
        var element = XElement.Parse(xml);

        // Act
        var result = element.Elements("child", "grandchild").ToList();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(1);
        result.Should().OnlyContain(x => x.Name == XName.Get("grandchild"));
    }
    
    [Test]
    public void Element_WhereElementExistsInWrongAncestor_ReturnsNull()
    {
        // Arrange
        var xml = "<root><wrong><grandchild /></wrong></root>";
        var element = XElement.Parse(xml);

        // Act
        var result = element.Element("child", "grandchild");

        // Assert
        result.Should().BeNull();
    }
    
    [Test]
    public void Elements_WhereElementsExistInWrongAncestor_ReturnsEmpty()
    {
        // Arrange
        var xml = "<root><wrong><grandchild /></wrong></root>";
        var element = XElement.Parse(xml);

        // Act
        var result = element.Elements("child", "grandchild").ToList();

        // Assert
        result.Should().BeEmpty();
    }
}