using System.Threading.Tasks;

namespace Mdk.CommandLine.Shared.Api
{
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
        /// The directory where the output should be written.
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
        /// <param name="text">The text content to write to the file.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        Task WriteAsync(string fileName, string text);

        /// <summary>
        /// Writes the specified text to a file in the trace directory (usually <c>obj</c>).
        /// </summary>
        /// <param name="fileName">The name of the file to write to, relative to the trace directory.</param>
        /// <param name="text">The text content to write to the file.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        Task WriteTraceAsync(string fileName, string text);

        /// <summary>
        /// Determines if the specified file exists.
        /// </summary>
        /// <param name="fileName">The file to check for.</param>
        /// <returns><c>true</c> if the file exists; otherwise, <c>false</c>.</returns>
        bool Exists(string fileName);

        /// <summary>
        /// Copies the specified file to the target location.
        /// </summary>
        /// <param name="sourceFile">The file to copy.</param>
        /// <param name="targetFile">The target location.</param>
        /// <param name="overwrite">Whether to overwrite the target file if it exists.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        Task CopyAsync(string sourceFile, string targetFile, bool overwrite = false);

        /// <summary>
        /// Creates a folder at the specified path.
        /// </summary>
        /// <param name="path">The path where the folder should be created.</param>
        /// <returns><c>true</c> if the folder was created; otherwise, <c>false</c> if it already exists.</returns>
        Task<bool> CreateFolderAsync(string path);
    }
}