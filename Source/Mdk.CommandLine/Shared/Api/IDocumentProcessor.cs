using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.Shared.Api;

/// <summary>
///     A processor that works on the combined syntax tree after all individual code files have been processed.
/// </summary>
public interface IDocumentProcessor
{
    /// <summary>
    ///     Processes the combined syntax tree after all individual code files have been processed.
    /// </summary>
    /// <param name="document">The combined document to process.</param>
    /// <param name="context">The context for the pack command, containing parameters and services useful for the processor.</param>
    /// <returns></returns>
    Task<Document> ProcessAsync(Document document, IPackContext context);
}