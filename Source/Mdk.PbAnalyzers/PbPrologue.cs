using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk2.PbAnalyzers
{
    /// <summary>
    ///     The using directives the programmable block wraps around every script before compiling it.
    /// </summary>
    /// <remarks>
    ///     The game builds this from more than one mechanism - plain namespace imports and type aliases at the time of
    ///     writing - so the extractor captures the generated source rather than the fields behind it, and this parses that
    ///     source as C#. A directive form nobody anticipated therefore still lands in the right bucket instead of being
    ///     silently dropped.
    /// </remarks>
    class PbPrologue
    {
        PbPrologue(HashSet<string> namespaces, HashSet<string> aliasedNamespaces, Dictionary<string, string> aliases)
        {
            Namespaces = namespaces;
            AliasedNamespaces = aliasedNamespaces;
            Aliases = aliases;
        }

        /// <summary>
        ///     Namespaces imported outright, so their types are usable by simple name.
        /// </summary>
        public HashSet<string> Namespaces { get; }

        /// <summary>
        ///     Namespaces that individual aliased types are drawn from. Importing one of these is not necessarily needed,
        ///     because the types the programmable block cares about are reachable through the aliases regardless.
        /// </summary>
        public HashSet<string> AliasedNamespaces { get; }

        /// <summary>
        ///     Alias name to fully qualified target, for the aliases the programmable block declares itself.
        /// </summary>
        public Dictionary<string, string> Aliases { get; }

        public static PbPrologue LoadEmbedded()
        {
            string text;
            using (var stream = typeof(PbPrologue).Assembly.GetManifestResourceStream("pbprologue.dat"))
            {
                if (stream == null)
                    throw new InvalidOperationException("Error loading the embedded programmable block prologue");
                using (var reader = new StreamReader(stream))
                    text = reader.ReadToEnd();
            }

            return Parse(text);
        }

        public static PbPrologue Parse(string text)
        {
            var namespaces = new HashSet<string>(StringComparer.Ordinal);
            var aliasedNamespaces = new HashSet<string>(StringComparer.Ordinal);
            var aliases = new Dictionary<string, string>(StringComparer.Ordinal);

            var root = CSharpSyntaxTree.ParseText(text).GetCompilationUnitRoot();
            foreach (var directive in root.Usings)
            {
                var name = directive.Name?.ToString();
                if (string.IsNullOrEmpty(name))
                    continue;

                if (directive.Alias != null)
                {
                    aliases[directive.Alias.Name.ToString()] = name;

                    // The alias names a type, so what matters for import purposes is the namespace holding it.
                    var lastDot = name.LastIndexOf('.');
                    if (lastDot > 0)
                        aliasedNamespaces.Add(name.Substring(0, lastDot));
                    continue;
                }

                // A static import would make that type's members available by simple name; treated as an import of the
                // type's own namespace, which is the closest thing that is true of it.
                if (directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
                {
                    var lastDot = name.LastIndexOf('.');
                    if (lastDot > 0)
                        aliasedNamespaces.Add(name.Substring(0, lastDot));
                    continue;
                }

                namespaces.Add(name);
            }

            return new PbPrologue(namespaces, aliasedNamespaces, aliases);
        }

        /// <summary>
        ///     Whether importing this namespace adds anything the programmable block does not already provide.
        /// </summary>
        public bool Provides(string namespaceName)
        {
            if (Namespaces.Contains(namespaceName))
                return true;

            // Everything the programmable block permits from an alias-only namespace is reachable through those aliases:
            // the whitelist for such a namespace covers exactly the aliased types. Should that ever stop being true the
            // effect is a missed warning rather than a false one, which is the right way round to be wrong.
            return AliasedNamespaces.Contains(namespaceName);
        }

        /// <summary>
        ///     Whether the script's own alias directive merely restates one the programmable block already declares.
        /// </summary>
        public bool DeclaresAlias(string alias, string target)
        {
            string existing;
            return Aliases.TryGetValue(alias, out existing) && string.Equals(existing, target, StringComparison.Ordinal);
        }

        public bool Imports(string namespaceName) => Namespaces.Contains(namespaceName);
    }
}
