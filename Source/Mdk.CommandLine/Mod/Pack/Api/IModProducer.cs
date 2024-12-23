using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Mdk.CommandLine.SharedApi;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.Mod.Pack.Api;

/// <summary>
///     Writes the final script content to the output directory.
/// </summary>
public interface IModProducer
{
    /// <summary>
    ///     Writes the final script content to the output directory.
    /// </summary>
    /// <param name="outputDirectory">The directory to output the script to.</param>
    /// <param name="script">The script content to output.</param>
    /// <param name="readmeDocument">An optional readme document to output.</param>
    /// <param name="thumbnailDocument">An optional thumbnail document to output.</param>
    /// <param name="context">The context for the pack command, containing parameters and services useful for the producer.</param>
    /// <returns></returns>
    Task<ImmutableArray<ProducedFile>> ProduceAsync(DirectoryInfo outputDirectory, StringBuilder script, TextDocument? readmeDocument, TextDocument? thumbnailDocument, IPackContext context);
}