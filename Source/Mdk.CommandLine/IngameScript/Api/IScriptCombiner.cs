using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;

namespace Mdk.CommandLine.IngameScript.Api;

/// <summary>
///     A combiner takes multiple code files and weaves them into a single syntax tree, in preparation
///     for creating the script file a Programmable Block can use.
/// </summary>
public interface IScriptCombiner
{
    /// <summary>
    ///     Combines the specified documents into a single syntax tree.
    /// </summary>
    /// <param name="syntaxTree">The syntax trees to combine.</param>
    /// <param name="metadata">Information about the project being processed.</param>
    /// <returns></returns>
    Task<CSharpSyntaxTree> CombineAsync(IReadOnlyList<CSharpSyntaxTree> syntaxTree, ScriptProjectMetadata metadata);
}