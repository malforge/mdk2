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
    static readonly ConditionalWeakTable<IPackContext, HashSet<string>> Entries = new();

    public static void Register(MemberDeclarationSyntax member, IPackContext context)
    {
        var entries = Entries.GetOrCreateValue(context);
        foreach (var enumDeclaration in member.DescendantNodesAndSelf().OfType<EnumDeclarationSyntax>())
        {
            var enumName = GetEnumIdentity(enumDeclaration);
            entries.Add(GetEnumKey(enumName));
            foreach (var enumMember in enumDeclaration.Members)
                entries.Add(GetEnumMemberKey(enumName, enumMember.Identifier.ValueText));
        }
    }

    public static bool Contains(ISymbol symbol, IPackContext context)
    {
        if (!Entries.TryGetValue(context, out var entries))
            return false;

        return symbol switch
        {
            INamedTypeSymbol { TypeKind: TypeKind.Enum } enumSymbol => entries.Contains(GetEnumKey(enumSymbol.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName))),
            IFieldSymbol { ContainingType.TypeKind: TypeKind.Enum } enumMemberSymbol => entries.Contains(GetEnumMemberKey(enumMemberSymbol.ContainingType.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName), enumMemberSymbol.Name)),
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
                parts.Push(typeDeclaration.Identifier.ValueText);
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
