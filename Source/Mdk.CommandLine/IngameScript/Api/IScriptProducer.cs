using System.IO;
using System.Text;
using System.Threading.Tasks;
using Mdk.CommandLine.SharedApi;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.IngameScript.Api;

/// <summary>
///     Writes the final script content to the output directory.
/// </summary>
public interface IScriptProducer
{
    /// <summary>
    ///     Writes the final script content to the output directory.
    /// </summary>
    /// <param name="outputDirectory">The directory to output the script to.</param>
    /// <param name="console">A console to output messages to.</param>
    /// <param name="script">The script content to output.</param>
    /// <param name="readmeDocument">An optional readme document to output.</param>
    /// <param name="thumbnailDocument">An optional thumbnail document to output.</param>
    /// <param name="metadata">The metadata for the script project.</param>
    /// <returns></returns>
    Task ProduceAsync(DirectoryInfo outputDirectory, IConsole console, StringBuilder script, TextDocument? readmeDocument, TextDocument? thumbnailDocument, ScriptProjectMetadata metadata);
}