using System.Linq;
using System.Xml.Linq;

namespace Mdk.CommandLine;

/// <summary>
/// Extension methods for <see cref="XDocument"/>.
/// </summary>
public static class XDocumentExtensions
{
    /// <summary>
    /// Finds the first element in the document that matches the specified path, going from the last element to the first.
    /// </summary>
    /// <remarks>
    /// For example: Matching the path <c>FindElementByPath("a", "b", "c")</c> might return:
    /// <ul>
    ///   <li><c>&lt;a&gt;&lt;b&gt;&lt;c&gt;</c></li>
    ///   <li><c>&lt;other&gt;&lt;a&gt;&lt;b&gt;&lt;c&gt;</c></li>
    /// </ul>
    /// </remarks>
    /// <param name="element"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public static XElement? FindByPath(this XElement element, params XName[] path)
    {
        return element.DescendantsAndSelf()
            .FirstOrDefault(e => MatchesPath(e, path));
    }

    static bool MatchesPath(XElement? element, XName[] path)
    {
        var n = path.Length - 1;
        while (element != null && n >= 0)
        {
            if (element.Name != path[n])
                return false;
            element = element.Parent;
            n--;
        }
        return true;
    }
}