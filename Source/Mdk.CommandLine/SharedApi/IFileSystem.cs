using System.Threading.Tasks;

namespace Mdk.CommandLine.SharedApi;

/// <summary>
/// Allows the writing of new files to the output. 
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// The path to the origin project file.
    /// </summary>
    string ProjectPath { get; }
    
    /// <summary>
    ///     The directory where the output should be written.
    /// </summary>
    string OutputDirectory { get; }
    
    /// <summary>
    /// The directory where trace files should be written.
    /// </summary>
    string TraceDirectory { get; }
    
    /// <summary>
    /// Writes the specified text to the file.
    /// </summary>
    /// <param name="fileName">The name of the file to write to, relative to the output directory.</param>
    /// <param name="text"></param>
    Task WriteAsync(string fileName, string text);
    
    /// <summary>
    /// Writes the specified text to a file in the trace directory (usually <c>obj</c>).
    /// </summary>
    /// <param name="fileName">The name of the file to write to, relative to the trace directory.</param>
    /// <param name="text"></param>
    Task WriteTraceAsync(string fileName, string text);
}