using System.Text;
using System.Threading.Tasks;
using Mdk.CommandLine.SharedApi;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.IngameScript.Pack.Api;

/// <summary>
///     Composes the final script from the syntax tree.
/// </summary>
public interface IScriptComposer
{
    /// <summary>
    ///     Composes the final script from the syntax tree.
    /// </summary>
    /// <param name="document">The document to compose the script from.</param>
    /// <param name="console">A console to output messages to.</param>
    /// <param name="metadata">The metadata for the script project.</param>
    /// <returns></returns>
    Task<StringBuilder> ComposeAsync(Document document, IConsole console, ScriptProjectMetadata metadata);
}