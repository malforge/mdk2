using System.Threading.Tasks;

namespace Mdk.Hub.Features.Shell;

/// <summary>
///     Represents an editor view that is hosted in a <see cref="HostWindow" />.
///     Implement this on view models that are opened as file editors.
/// </summary>
public interface IFileEditor
{
    /// <summary>
    ///     Opens the specified file in this editor.
    /// </summary>
    /// <param name="filePath">Path to the file to open.</param>
    Task OpenFileAsync(string filePath);
}
