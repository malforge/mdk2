using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

static class PreservedDeclarationRegistry
{
    static readonly ConditionalWeakTable<IPackContext, HashSet<string>> Entries = new();

    public static void Register(MemberDeclarationSyntax member, IPackContext context)
    {
        var entries = Entries.GetOrCreateValue(context);
        foreach (var enumDeclaration in member.DescendantNodesAndSelf().OfType<EnumDeclarationSyntax>())
        {
            entries.Add(GetEnumKey(enumDeclaration.Identifier.ValueText));
            foreach (var enumMember in enumDeclaration.Members)
                entries.Add(GetEnumMemberKey(enumMember.Identifier.ValueText));
        }
    }

    public static bool Contains(ISymbol symbol, IPackContext context)
    {
        if (!Entries.TryGetValue(context, out var entries))
            return false;

        return symbol switch
        {
            INamedTypeSymbol { TypeKind: TypeKind.Enum } enumSymbol => entries.Contains(GetEnumKey(enumSymbol.Name)),
            IFieldSymbol { ContainingType.TypeKind: TypeKind.Enum } enumMemberSymbol => entries.Contains(GetEnumMemberKey(enumMemberSymbol.Name)),
            _ => false
        };
    }

    static string GetEnumKey(string name) => $"enum:{name}";
    static string GetEnumMemberKey(string name) => $"enum-member:{name}";
}
