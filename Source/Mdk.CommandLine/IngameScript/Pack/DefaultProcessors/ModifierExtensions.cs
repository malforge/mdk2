using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

/// <summary>
///     Extensions for removing modifiers from declarations without disturbing their layout.
/// </summary>
public static class ModifierExtensions
{
    /// <summary>
    ///     Removes the first modifier of the given kind from the declaration, if it has one.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="node"></param>
    /// <param name="kind"></param>
    /// <returns></returns>
    public static T WithoutModifier<T>(this T node, SyntaxKind kind) where T : MemberDeclarationSyntax
    {
        var index = node.Modifiers.IndexOf(kind);
        return index < 0 ? node : node.WithoutModifierAt(index);
    }

    /// <summary>
    ///     Removes the modifier at the given index from the declaration. The leading trivia of the removed keyword is
    ///     moved to the first remaining modifier, or to the declaration itself when no modifiers remain, so that the
    ///     declaration keeps its indentation and anything written in front of it.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="node"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static T WithoutModifierAt<T>(this T node, int index) where T : MemberDeclarationSyntax
    {
        var modifiers = RemoveModifierAt(node.Modifiers, index, out var orphanedTrivia);
        var result = (T)node.WithModifiers(modifiers);
        return orphanedTrivia.Count > 0 ? result.WithLeadingTrivia(orphanedTrivia) : result;
    }

    /// <summary>
    ///     Removes every accessibility modifier from the accessor. An accessor may only be more restrictive than the
    ///     property it belongs to, so its modifier has to go when the property loses its own.
    /// </summary>
    /// <param name="accessor"></param>
    /// <returns></returns>
    public static AccessorDeclarationSyntax WithoutAccessibilityModifiers(this AccessorDeclarationSyntax accessor)
    {
        while (true)
        {
            var index = IndexOfAccessibilityModifier(accessor.Modifiers);
            if (index < 0)
                return accessor;
            var modifiers = RemoveModifierAt(accessor.Modifiers, index, out var orphanedTrivia);
            accessor = accessor.WithModifiers(modifiers);
            if (orphanedTrivia.Count > 0)
                accessor = accessor.WithLeadingTrivia(orphanedTrivia);
        }
    }

    static int IndexOfAccessibilityModifier(SyntaxTokenList modifiers)
    {
        for (var index = 0; index < modifiers.Count; index++)
        {
            if (modifiers[index].IsKind(SyntaxKind.PublicKeyword)
                || modifiers[index].IsKind(SyntaxKind.PrivateKeyword)
                || modifiers[index].IsKind(SyntaxKind.ProtectedKeyword)
                || modifiers[index].IsKind(SyntaxKind.InternalKeyword))
                return index;
        }
        return -1;
    }

    /// <summary>
    ///     Removes the modifier at the given index, moving its leading trivia to the first remaining modifier. When
    ///     nothing remains, that trivia is handed back to the caller to put on the declaration instead.
    /// </summary>
    /// <param name="modifiers"></param>
    /// <param name="index"></param>
    /// <param name="orphanedTrivia"></param>
    /// <returns></returns>
    static SyntaxTokenList RemoveModifierAt(SyntaxTokenList modifiers, int index, out SyntaxTriviaList orphanedTrivia)
    {
        var removed = modifiers[index];
        var remaining = modifiers.RemoveAt(index);
        if (remaining.Count == 0)
        {
            orphanedTrivia = removed.LeadingTrivia;
            return remaining;
        }

        orphanedTrivia = default;
        return remaining.Replace(remaining[0], remaining[0].WithLeadingTrivia(
            removed.LeadingTrivia.Concat(remaining[0].LeadingTrivia)));
    }
}
