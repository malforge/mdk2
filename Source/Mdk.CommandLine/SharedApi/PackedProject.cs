using System.Collections.Immutable;

namespace Mdk.CommandLine.SharedApi;

/// <summary>
/// A packed project.
/// </summary>
/// <param name="name"></param>
/// <param name="producedFiles"></param>
public readonly struct PackedProject(string name, ImmutableArray<ProducedFile> producedFiles)
{
    /// <summary>
    /// The name of the packed project.
    /// </summary>
    public string Name { get; } = name;
        
    /// <summary>
    /// The files produced by the pack operation.
    /// </summary>
    public ImmutableArray<ProducedFile> ProducedFiles { get; } = producedFiles;
}