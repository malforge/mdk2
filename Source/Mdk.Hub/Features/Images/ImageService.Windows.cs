using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DirectXTexNet;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.Images;

/// <summary>
///     Windows implementation of <see cref="IImageService" />.
///     Uses DirectXTexNet to decode DDS files (including BC7) into RGBA pixels
///     and wraps them in an Avalonia <see cref="WriteableBitmap" />.
///     SE DDS files use premultiplied alpha, which Avalonia expects natively.
///     Results are cached for the lifetime of the application.
///     At most 4 decodes run concurrently to avoid saturating the CPU.
/// </summary>
[Singleton<IImageService>]
public class ImageService : IImageService
{
    static readonly SemaphoreSlim Throttle = new(4, 4);

    readonly ConcurrentDictionary<string, (DateTime LastWrite, Lazy<Task<Bitmap?>> Loader)> _cache
        = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<Bitmap?> LoadDdsAsync(string path)
    {
        var lastWrite = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        var entry = _cache.AddOrUpdate(
            path,
            static (p, lw) => (lw, new Lazy<Task<Bitmap?>>(() => DecodeAsync(p))),
            (p, existing, lw) => existing.LastWrite == lw
                ? existing
                : (lw, new Lazy<Task<Bitmap?>>(() => DecodeAsync(p))),
            lastWrite);
        return entry.Loader.Value;
    }

    static async Task<Bitmap?> DecodeAsync(string path)
    {
        await Throttle.WaitAsync();
        try
        {
            return await Task.Run<Bitmap?>(() =>
            {
                try
                {
                if (!File.Exists(path))
                    return null;

                using var scratch = TexHelper.Instance.LoadFromDDSFile(path, DDS_FLAGS.NONE);
                var meta = scratch.GetMetadata();

                // Decompress to plain RGBA if the source is block-compressed (e.g. BC7).
                ScratchImage? decompressed = null;
                ScratchImage working;
                if (TexHelper.Instance.IsCompressed(meta.Format))
                {
                    decompressed = scratch.Decompress(DXGI_FORMAT.R8G8B8A8_UNORM);
                    working = decompressed;
                }
                else if (meta.Format != DXGI_FORMAT.R8G8B8A8_UNORM)
                {
                    decompressed = scratch.Convert(DXGI_FORMAT.R8G8B8A8_UNORM, TEX_FILTER_FLAGS.DEFAULT, 0.5f);
                    working = decompressed;
                }
                else
                {
                    working = scratch;
                }

                try
                {
                    var image = working.GetImage(0);
                    var width = image.Width;
                    var height = image.Height;
                    var rowPitch = image.RowPitch;

                    // Use the DX10 alpha mode when available; SE files are typically UNKNOWN,
                    // which we treat as premultiplied since that's what SE uses.
                    var alphaFormat = meta.GetAlphaMode() == TEX_ALPHA_MODE.STRAIGHT
                        ? AlphaFormat.Unpremul
                        : AlphaFormat.Premul;

                    var bitmap = new WriteableBitmap(
                        new Avalonia.PixelSize(width, height),
                        new Avalonia.Vector(96, 96),
                        PixelFormats.Rgba8888,
                        alphaFormat);

                    using var fb = bitmap.Lock();
                    var dstRowPitch = fb.RowBytes;
                    var copyPitch = Math.Min(rowPitch, dstRowPitch);
                    unsafe
                    {
                        var src = (byte*)image.Pixels;
                        var dst = (byte*)fb.Address;
                        for (var y = 0; y < height; y++)
                            Buffer.MemoryCopy(src + y * rowPitch, dst + y * dstRowPitch, dstRowPitch, copyPitch);
                    }

                    return bitmap;
                }
                finally
                {
                    decompressed?.Dispose();
                }
            }
            catch
            {
                return null;
            }
        });
        }
        finally
        {
            Throttle.Release();
        }
    }
}
