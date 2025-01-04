using System.Text;
using System.Threading.Tasks;
using Mdk.CommandLine.Shared.Api;
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
    /// <param name="context">The context for the pack command, containing parameters and services useful for the composer.</param>
    /// <returns></returns>
    Task<StringBuilder> ComposeAsync(Document document, IPackContext context);
}