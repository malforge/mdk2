using System.Text;
using Mdk.DocGen3.Pages;
using RazorLight;

namespace Mdk.DocGen3;

public class Context(RazorLightEngine engine, string outputFolder, IEnumerable<DocumentationPage> pages)
{
    readonly HtmlPrettyPrinter _printer = new();
    public RazorLightEngine Engine { get; } = engine;
    public string OutputFolder { get; } = outputFolder;
    public IReadOnlyList<DocumentationPage> Pages { get; } = pages.ToList();

    public void WriteHtml(string fileName, string content) => WriteAllText(fileName, _printer.Reformat(content));

    public void WriteAllText(string fileName, string content)
    {
        fileName = Path.GetFullPath(Path.Combine(OutputFolder, fileName));
        var directory = Path.GetDirectoryName(fileName);
        if (directory != null) Directory.CreateDirectory(directory);
        File.WriteAllText(fileName, content);
    }

    public string? GetCustomDescription(string key) => null;

    /// <summary>
    ///     Generates a relative path from the current path to the desired path.
    /// </summary>
    /// <param name="currentPathSlug"></param>
    /// <param name="desiredPathSlug"></param>
    /// <returns></returns>
    public string ToRelative(string currentPathSlug, string desiredPathSlug)
    {
        currentPathSlug = currentPathSlug.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        desiredPathSlug = desiredPathSlug.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var currentPath = Path.GetFullPath(Path.Combine(OutputFolder, currentPathSlug));
        var desiredPath = Path.GetFullPath(Path.Combine(OutputFolder, desiredPathSlug));
        if (currentPath == desiredPath) return desiredPathSlug;
        var relativePath = Path.GetRelativePath(Path.GetDirectoryName(currentPath) ?? "", desiredPath);
        if (string.IsNullOrEmpty(relativePath)) return desiredPathSlug;
        if (relativePath.StartsWith(Path.DirectorySeparatorChar.ToString()))
            relativePath = relativePath.Substring(1);
        relativePath = relativePath.Replace(Path.DirectorySeparatorChar, '/');
        return relativePath;
    }

    public string ToSafeFileName(string unsafeName)
    {
        // Replace invalid characters for file names
        var sb = new StringBuilder();
        foreach (var c in unsafeName)
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.')
                sb.Append(c);
            else
                sb.Append('_'); // Replace invalid characters with underscore
        }
        return sb.ToString();
    }

    public string? GetCommunityRemarksHtml(string docKey) => null;
}