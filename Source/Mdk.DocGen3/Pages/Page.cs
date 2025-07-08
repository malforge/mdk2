using System.Text;
using Mdk.DocGen3.Types;

namespace Mdk.DocGen3.Pages;

public abstract class Page
{
    string? _fileName;

    public string Url
    {
        get
        {
            if (_fileName == null)
            {
                var ns = GetMemberDocumentation().Namespace.Replace('.', '/');
                _fileName = Path.Combine(ns, GetSafeFileName(GetMemberDocumentation().Title) + ".html");
            }
            return _fileName;
        }
    }

    protected abstract IMemberDocumentation GetMemberDocumentation();

    protected static string GetSafeFileName(string name)
    {
        // Replace invalid characters for file names
        StringBuilder sb = new();
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.')
                sb.Append(c);
            else
                sb.Append('_'); // Replace invalid characters with underscore
        }
        return sb.ToString();
    }
}