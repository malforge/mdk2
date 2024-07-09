using System;

namespace Mal.DocumentGenerator.Whitelists;

public class WhitelistRule
{
    public WhitelistRule(string whitelistRule)
    {
        var lastComma = whitelistRule.LastIndexOf(',');
        if (lastComma == -1)
            throw new Exception("Invalid whitelist rule: " + whitelistRule);

        var path = whitelistRule.Substring(0, lastComma).Trim();
        AssemblyName = whitelistRule.Substring(lastComma + 1).Trim();
        var anyMember = path.EndsWith(".*") || path.EndsWith("+*");
        if (anyMember)
            path = path.Substring(0, path.Length - 2);
        Path = path;
        AnyMember = anyMember;
    }

    public string AssemblyName { get; }
    public string Path { get; }
    public bool AnyMember { get; }

    public bool IsMatch(string assemblyName, string typeName)
    {
        if (AssemblyName != assemblyName)
            return false;
        typeName = typeName.Replace("+", ".");
        if (AnyMember)
            return typeName.StartsWith(Path);
        return typeName == Path;
    }
}