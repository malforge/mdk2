using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Mdk.CommandLine.Utility;

/// <summary>
///     Extension methods for <see cref="XElement" />.
/// </summary>
public static class XElementExtensions
{
    /// <summary>
    ///     Finds the first element which matches the given name path and namespace.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="firstName"></param>
    /// <param name="otherNames"></param>
    /// <returns></returns>
    public static XElement? Element(this XContainer container, XName firstName, params XName[] otherNames)
    {
        var names = new[] { firstName }.Concat(otherNames.Select(n => n)).ToList();
        return Element(container, names, 0);
    }

    /// <summary>
    ///     Finds the first element which matches the given name path and namespace.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="ns"></param>
    /// <param name="firstName"></param>
    /// <param name="otherNames"></param>
    /// <returns></returns>
    public static XElement? Element(this XContainer container, XNamespace ns, string firstName, params string[] otherNames)
    {
        var names = new[] { XName.Get(firstName, ns.NamespaceName) }.Concat(otherNames.Select(n => XName.Get(n, ns.NamespaceName))).ToList();
        return Element(container, names, 0);
    }

    static XElement? Element(XContainer? container, List<XName> names, int depth)
    {
        if (container == null)
            return null;
        var n = depth + 1;
        foreach (var element in container.Elements(names[depth]))
        {
            if (names.Count == n)
                return element;

            var next = Element(element, names, n);
            if (next != null)
                return next;
        }

        return null;
    }

    static IEnumerable<XElement> Elements(XContainer? container, List<XName> names, int depth)
    {
        if (container == null || depth >= names.Count)
            yield break;
        var n = depth + 1;
        foreach (var element in container.Elements(names[depth]))
        {
            if (names.Count == n)
                yield return element;

            foreach (var selection in Elements(element, names, n))
                yield return selection;
        }
    }

    /// <summary>
    ///     Finds all elements which match the given name path and namespace.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="firstName"></param>
    /// <param name="otherNames"></param>
    /// <returns></returns>
    public static IEnumerable<XElement> Elements(this XContainer container, XName firstName, params XName[] otherNames)
    {
        var names = new[] { firstName }.Concat(otherNames.Select(n => n)).ToList();
        return Elements(container, names, 0);
    }

    /// <summary>
    ///     Finds all elements which match the given name path and namespace.
    /// </summary>
    /// <param name="container"></param>
    /// <param name="ns"></param>
    /// <param name="firstName"></param>
    /// <param name="otherNames"></param>
    /// <returns></returns>
    public static IEnumerable<XElement> Elements(this XContainer container, XNamespace ns, string firstName, params string[] otherNames)
    {
        var names = new[] { XName.Get(firstName, ns.NamespaceName) }.Concat(otherNames.Select(n => XName.Get(n, ns.NamespaceName))).ToList();
        return Elements(container, names, 0);
    }

    /// <summary>
    ///     Adds an attribute to the element in a fluent manner.
    /// </summary>
    /// <param name="element"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static XElement AddAttribute(this XElement element, XName name, string value)
    {
        element.SetAttributeValue(name, value);
        return element;
    }

    /// <summary>
    ///     Filters the elements by the given attribute name and value.
    /// </summary>
    /// <param name="elements"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <param name="comparison"></param>
    /// <returns></returns>
    public static IEnumerable<XElement> WithAttribute(this IEnumerable<XElement> elements, XName name, string value, StringComparison comparison = StringComparison.OrdinalIgnoreCase) => elements.Where(e => e.Attribute(name)?.Value.Equals(value, comparison) == true);

    /// <summary>
    ///    Filters the elements by the given attribute name and value.
    /// </summary>
    /// <param name="elements"></param>
    /// <param name="name"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public static IEnumerable<XElement> WithAttribute(this IEnumerable<XElement> elements, XName name, Func<XAttribute, bool> predicate)
    {
        foreach (var e in elements)
        {
            var attribute = e.Attribute(name);
            if (attribute != null && predicate(attribute))
                yield return e;
        }
    }


    /// <summary>
    ///     Adds a child element to the element in a fluent manner.
    /// </summary>
    /// <param name="element"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static XElement WithElement(this XElement element, XName name, string value)
    {
        element.Add(new XElement(name, value));
        return element;
    }

    /// <summary>
    ///     Adds a child element to the element in a fluent manner.
    /// </summary>
    /// <param name="element"></param>
    /// <param name="child"></param>
    /// <returns></returns>
    public static XElement WithElement(this XElement element, XElement child)
    {
        element.Add(child);
        return element;
    }
}