using System.Text;
using System.Threading.Tasks;
using Mdk.CommandLine.Shared.Api;

namespace Mdk.CommandLine.IngameScript.Pack.Api;

/// <summary>
///     A processor that runs after the script has been composed.
/// </summary>
public interface IScriptPostCompositionProcessor
{
    /// <summary>
    ///     Processes the script after it has been composed.
    /// </summary>
    /// <param name="script">The script to process.</param>
    /// <param name="context">The context for the pack command, containing parameters and services useful for the processor.</param>
    /// <returns></returns>
    Task<StringBuilder> ProcessAsync(StringBuilder script, IPackContext context);
}