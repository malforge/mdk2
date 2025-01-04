using System.Linq;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.Mod.Pack.DefaultProcessors;

/// <summary>
///     Extensions dealing with the MDK analysis annotations
/// </summary>
public static class AnnotationExtensions
{
    /// <summary>
    ///     Determines whether the given syntax node should be preserved from major changes.
    /// </summary>
    /// <param name="syntaxNode"></param>
    /// <returns></returns>
    public static bool ShouldBePreserved(this SyntaxNode syntaxNode) => syntaxNode.GetAnnotations("MDK").Any(a => a.Data?.Contains("preserve") ?? false);

    /// <summary>
    ///     Determines whether the given syntax trivia should be preserved from major changes.
    /// </summary>
    /// <param name="syntaxTrivia"></param>
    /// <returns></returns>
    public static bool ShouldBePreserved(this SyntaxTrivia syntaxTrivia) => syntaxTrivia.GetAnnotations("MDK").Any(a => a.Data?.Contains("preserve") ?? false);

    /// <summary>
    ///     Determines whether the given syntax token should be preserved from major changes.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public static bool ShouldBePreserved(this SyntaxToken token) => token.GetAnnotations("MDK").Any(a => a.Data?.Contains("preserve") ?? false);
}