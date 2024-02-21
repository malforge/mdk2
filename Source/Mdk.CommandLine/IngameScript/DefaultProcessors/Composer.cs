using System.Text;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Api;
using Mdk.CommandLine.SharedApi;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.IngameScript.DefaultProcessors;

/// <summary>
///     The default script composer.
/// </summary>
public class Composer : IScriptComposer
{
    /// <inheritdoc />
    public async Task<StringBuilder> ComposeAsync(Document document, IConsole console, ScriptProjectMetadata metadata)
    {
        var builder = new StringBuilder();
        var root = await document.GetSyntaxRootAsync();
        builder.Append(root?.ToFullString());
        return builder;
    }
}