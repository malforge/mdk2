using System.Text;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Api;
using Mdk.CommandLine.SharedApi;
using Microsoft.CodeAnalysis.CSharp;

namespace Mdk.CommandLine.IngameScript.DefaultProcessors;

/// <summary>
///     The default script composer.
/// </summary>
public class ScriptComposer : IScriptComposer
{
    /// <inheritdoc />
    public Task<StringBuilder> ComposeAsync(CSharpSyntaxTree syntaxTree, IConsole console, ScriptProjectMetadata metadata) => Task.FromResult(new StringBuilder(syntaxTree.ToString()));
}