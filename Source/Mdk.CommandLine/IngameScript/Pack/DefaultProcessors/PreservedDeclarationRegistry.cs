using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using Mdk.CommandLine.Shared.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

static class PreservedDeclarationRegistry
{
    static readonly ConditionalWeakTable<IPackContext, ConcurrentDictionary<string, byte>> Entries = new();

    public static void Register(MemberDeclarationSyntax member, IPackContext context)
    {
        var entries = Entries.GetOrCreateValue(context);
        foreach (var enumDeclaration in member.DescendantNodesAndSelf().OfType<EnumDeclarationSyntax>())
        {
            var enumName = GetEnumIdentity(enumDeclaration);
            entries.TryAdd(GetEnumKey(enumName), 0);
            foreach (var enumMember in enumDeclaration.Members)
                entries.TryAdd(GetEnumMemberKey(enumName, enumMember.Identifier.ValueText), 0);
        }
    }

    public static bool Contains(ISymbol symbol, IPackContext context)
    {
        if (!Entries.TryGetValue(context, out var entries))
            return false;

        return symbol switch
        {
            INamedTypeSymbol { TypeKind: TypeKind.Enum } enumSymbol => entries.ContainsKey(GetEnumKey(enumSymbol.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName))),
            IFieldSymbol { ContainingType.TypeKind: TypeKind.Enum } enumMemberSymbol => entries.ContainsKey(GetEnumMemberKey(enumMemberSymbol.ContainingType.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName), enumMemberSymbol.Name)),
            _ => false
        };
    }

    static string GetEnumKey(string name) => $"enum:{name}";
    static string GetEnumMemberKey(string enumName, string memberName) => $"enum-member:{enumName}.{memberName}";

    static string GetEnumIdentity(EnumDeclarationSyntax enumDeclaration)
    {
        var parts = new Stack<string>();
        parts.Push(enumDeclaration.Identifier.ValueText);

        for (var parent = enumDeclaration.Parent; parent != null; parent = parent.Parent)
        {
            if (parent is TypeDeclarationSyntax typeDeclaration)
                parts.Push($"{typeDeclaration.Identifier}{typeDeclaration.TypeParameterList}");
        }

        var builder = new StringBuilder();
        while (parts.Count > 0)
        {
            if (builder.Length > 0)
                builder.Append('.');
            builder.Append(parts.Pop());
        }

        return builder.ToString();
    }
}
