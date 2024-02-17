﻿using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;

namespace Mdk.CommandLine.IngameScript.Api;

/// <summary>
/// A processor that works on the combined syntax tree after all individual code files have been processed.
/// </summary>
public interface IScriptPostprocessor
{
    /// <summary>
    /// Processes the combined syntax tree after all individual code files have been processed.
    /// </summary>
    /// <param name="syntaxTree">The combined syntax tree to process.</param>
    /// <param name="metadata">Information about the project being processed.</param>
    /// <returns></returns>
    Task<CSharpSyntaxTree> ProcessAsync(CSharpSyntaxTree syntaxTree, ScriptProjectMetadata metadata);
}