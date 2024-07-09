using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mal.DocumentGenerator.Whitelists;

public class Whitelist
{
    readonly List<WhitelistRule> _blacklist = new();
    readonly List<WhitelistRule> _rules = new();

    public static Whitelist Load(string fileName)
    {
        var lines = File.ReadAllLines(fileName);
        var whitelist = new Whitelist();
        foreach (var line in lines)
            whitelist._rules.Add(new WhitelistRule(line));
        return whitelist;
    }

    public void AddWhitelist(string whitelistRule) => _rules.Add(new WhitelistRule(whitelistRule));

    public void AddBlacklist(string blacklistRule) => _blacklist.Add(new WhitelistRule(blacklistRule));

    public bool IsWhitelisted(string assemblyName, string typeName) => _rules.Any(rule => rule.IsMatch(assemblyName, typeName)) && !_blacklist.Any(rule => rule.IsMatch(assemblyName, typeName));

    public bool IsAssemblyWhitelisted(string assemblyName) => _rules.Any(rule => rule.AssemblyName == assemblyName);
}