using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.IngameScript.Pack.Api;

/// <summary>
///     Writes the final script content to the output directory.
/// </summary>
public interface IScriptProducer
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

    /// <summary>
    /// Represents a file that was produced by the script producer.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="path"></param>
    /// <param name="content"></param>
    public readonly struct ProducedFile(string? id, string path, string? content)
    {
        /// <summary>
        /// The ID of the produced file.
        /// </summary>
        public string? Id { get; } = id;
        
        /// <summary>
        /// The absolute path of the produced file.
        /// </summary>
        public string Path { get; } = path;
        
        /// <summary>
        /// The content of a produced file, unless it was simply copied from another location.
        /// </summary>
        public string? Content { get; } = content;
    }
}