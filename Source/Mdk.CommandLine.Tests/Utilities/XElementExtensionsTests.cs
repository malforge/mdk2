using System.Xml.Linq;
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
        var result = element.Element("", "child", "grandchild");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo(XName.Get("grandchild")));
    }

    [Test]
    public void Elements_WhereElementsExist_ReturnsElements()
    {
        // Arrange
        var xml = "<root><child><grandchild /></child><child><grandchild /></child></root>";
        var element = XElement.Parse(xml);

        // Act
        var result = element.Elements("", "child", "grandchild").ToList();

        // Assert
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result, Has.All.Matches<XElement>(x => x.Name == XName.Get("grandchild")));
    }

    [Test]
    public void Element_WhereElementExistsInAnotherChildInstance_ReturnsElement()
    {
        // Arrange
        var xml = "<root><child></child><child><grandchild /></child></root>";
        var element = XElement.Parse(xml);

        // Act
        var result = element.Element("", "child", "grandchild");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo(XName.Get("grandchild")));
    }
    
    [Test]
    public void Elements_WhereElementsExistsInAnotherChildInstance_ReturnsElements()
    {
        // Arrange
        var xml = "<root><child></child><child><grandchild /></child></root>";
        var element = XElement.Parse(xml);

        // Act
        var result = element.Elements("", "child", "grandchild").ToList();

        // Assert
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result, Has.All.Matches<XElement>(x => x.Name == XName.Get("grandchild")));
    }
    
    [Test]
    public void Element_WhereElementExistsInWrongAncestor_ReturnsNull()
    {
        // Arrange
        var xml = "<root><wrong><grandchild /></wrong></root>";
        var element = XElement.Parse(xml);

        // Act
        var result = element.Element("", "child", "grandchild");

        // Assert
        Assert.That(result, Is.Null);
    }
    
    [Test]
    public void Elements_WhereElementsExistInWrongAncestor_ReturnsEmpty()
    {
        // Arrange
        var xml = "<root><wrong><grandchild /></wrong></root>";
        var element = XElement.Parse(xml);

        // Act
        var result = element.Elements("", "child", "grandchild").ToList();

        // Assert
        Assert.That(result, Is.Empty);
    }
}