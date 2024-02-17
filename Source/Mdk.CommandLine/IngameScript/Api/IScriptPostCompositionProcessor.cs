using System.Text;
using System.Threading.Tasks;

namespace Mdk.CommandLine.IngameScript.Api;

/// <summary>
///     A processor that runs after the script has been composed.
/// </summary>
public interface IScriptPostCompositionProcessor
{
    /// <summary>
    ///     Processes the script after it has been composed.
    /// </summary>
    /// <param name="script">The script to process.</param>
    /// <param name="metadata">The metadata for the script project.</param>
    /// <returns></returns>
    Task<StringBuilder> ProcessAsync(StringBuilder script, ScriptProjectMetadata metadata);
}