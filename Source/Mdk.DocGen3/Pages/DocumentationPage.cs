using System.Diagnostics;
using System.Text;
using Mdk.DocGen3.CodeSecurity;
using Mdk.DocGen3.Support;
using Mdk.DocGen3.Types;

namespace Mdk.DocGen3.Pages;

public abstract class DocumentationPage
{
    string? _fileName;

    public string Url
    {
        get
        {
            if (_fileName == null)
            {
                var ns = GetMemberDocumentation().Namespace.Replace('.', '/');
                _fileName = Path.Combine(ns, GetSafeFileName(GetMemberDocumentation().Name) + ".html");
            }
            return _fileName;
        }
    }
    
    public string Title => GetMemberDocumentation().Title;
    public string Name => GetMemberDocumentation().Name;
    public string Namespace => GetMemberDocumentation().Namespace;

    public abstract IMemberDocumentation GetMemberDocumentation();

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

    public virtual bool IsIgnored(Whitelist whitelist)
    {
        var member = GetMemberDocumentation();
        // if (member.Namespace.Contains("Linq")) Debugger.Break();
        if (member.Member.IsMsType())
            return true;

        if (!whitelist.IsAllowed(member.WhitelistKey))
            return true;

        return false;
    }
}