using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace Mdk.Hub.Features.Images;

/// <summary>
///     Loads DDS image files from disk and returns Avalonia bitmaps.
///     Returns <c>null</c> when the file cannot be loaded or the format is unsupported.
/// </summary>
public interface IImageService
{
    /// <summary>
    ///     Asynchronously loads a DDS image from <paramref name="path" />.
    /// </summary>
    /// <param name="path">Absolute path to the .dds file.</param>
    /// <returns>The decoded bitmap, or <c>null</c> on failure.</returns>
    Task<Bitmap?> LoadDdsAsync(string path);
}
