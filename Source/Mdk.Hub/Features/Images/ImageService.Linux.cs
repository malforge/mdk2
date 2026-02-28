using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.Images;

/// <summary>
///     Linux stub for <see cref="IImageService" />. DDS decoding is not available on Linux;
///     callers receive <c>null</c> and should display a placeholder instead.
/// </summary>
[Singleton<IImageService>]
public class ImageService : IImageService
{
    /// <inheritdoc />
    public Task<Bitmap?> LoadDdsAsync(string path) => Task.FromResult<Bitmap?>(null);
}
