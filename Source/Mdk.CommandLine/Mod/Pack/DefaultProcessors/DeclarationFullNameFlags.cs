using System;

namespace Mdk.CommandLine.Mod.Pack.DefaultProcessors;

/// <summary>
///     Flags altering the behavior of <see cref="AnalysisExtensions.GetFullName(Microsoft.CodeAnalysis.ISymbol,Mdk.CommandLine.Mod.Pack.DefaultProcessors.DeclarationFullNameFlags)" /> and
///     <see cref="AnalysisExtensions.GetFullName(Microsoft.CodeAnalysis.CSharp.Syntax.MemberDeclarationSyntax?,Mdk.CommandLine.Mod.Pack.DefaultProcessors.DeclarationFullNameFlags)" />
/// </summary>
[Flags]
public enum DeclarationFullNameFlags
{
    /// <summary>
    ///     Default behavior. Returns a complete, fully qualified name, including the namespace.
    /// </summary>
    Default = 0b0000,

    /// <summary>
    ///     Only generates the name up to the outermost type declaration. Does not include the namespace.
    /// </summary>
    WithoutNamespaceName = 0b0001
}