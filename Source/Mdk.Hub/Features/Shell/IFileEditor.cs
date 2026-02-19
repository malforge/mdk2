namespace Mdk.Hub.Features.Shell;

/// <summary>
///     Interface for ViewModels that can open and edit files.
/// </summary>
public interface IFileEditor
{
    /// <summary>
    ///     Opens the specified file in this editor.
    /// </summary>
    /// <param name="filePath">Path to the file to open (may not exist yet).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    System.Threading.Tasks.Task OpenFileAsync(string filePath);
}
