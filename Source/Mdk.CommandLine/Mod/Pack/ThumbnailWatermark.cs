using System;
using System.IO;
using Mdk.CommandLine.Shared.Api;
using SkiaSharp;

namespace Mdk.CommandLine.Mod.Pack;

/// <summary>
///     Stamps a diagonal red watermark across a mod thumbnail, used to visually distinguish branch-specific
///     (e.g. alpha) builds. Best-effort: any failure (unreadable image, no available font, etc.) leaves the
///     destination untouched and reports <c>false</c> so the caller can fall back to a plain copy.
/// </summary>
public static class ThumbnailWatermark
{
    /// <summary>
    ///     Reads <paramref name="sourcePath" />, draws <paramref name="text" /> across it in large red letters,
    ///     and writes the result to <paramref name="destinationPath" /> as a PNG.
    /// </summary>
    /// <returns><c>true</c> if the watermarked thumbnail was written; <c>false</c> on any failure.</returns>
    public static bool TryStamp(string sourcePath, string destinationPath, string text, IConsole console)
    {
        try
        {
            using var original = SKBitmap.Decode(sourcePath);
            if (original == null)
            {
                console.Print($"Could not decode thumbnail '{sourcePath}' for watermarking; using it unmodified.");
                return false;
            }

            using var typeface = ResolveTypeface();
            if (typeface == null)
            {
                console.Print("No system font available to render the thumbnail watermark; using the thumbnail unmodified.");
                return false;
            }

            var width = original.Width;
            var height = original.Height;

            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.DrawBitmap(original, 0, 0);

            using var fill = new SKPaint
            {
                IsAntialias = true,
                Typeface = typeface,
                TextAlign = SKTextAlign.Center,
                Color = new SKColor(220, 30, 30, 90),
                Style = SKPaintStyle.Fill
            };
            using var outline = new SKPaint
            {
                IsAntialias = true,
                Typeface = typeface,
                TextAlign = SKTextAlign.Center,
                Color = new SKColor(20, 0, 0, 90),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = Math.Max(1f, width * 0.006f)
            };

            // Size the text so it spans roughly the image diagonal, then rotate to sit corner-to-corner.
            var diagonal = (float)Math.Sqrt((width * (double)width) + (height * (double)height));
            fill.TextSize = 10f;
            var measured = fill.MeasureText(text);
            if (measured <= 0f)
                return false;
            var scale = diagonal * 0.82f / measured;
            fill.TextSize *= scale;
            outline.TextSize = fill.TextSize;

            var angle = -(float)(Math.Atan2(height, width) * 180.0 / Math.PI);
            canvas.Save();
            canvas.RotateDegrees(angle, width / 2f, height / 2f);

            // Vertically center on the text's visual middle.
            var metrics = fill.FontMetrics;
            var baselineY = (height / 2f) - ((metrics.Ascent + metrics.Descent) / 2f);
            canvas.DrawText(text, width / 2f, baselineY, outline);
            canvas.DrawText(text, width / 2f, baselineY, fill);
            canvas.Restore();

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using (var output = File.Create(destinationPath))
                data.SaveTo(output);
            return true;
        }
        catch (Exception e)
        {
            console.Print($"Could not watermark thumbnail: {e.Message}. Using the thumbnail unmodified.");
            return false;
        }
    }

    /// <summary>
    ///     Resolves a bold typeface from the host's installed fonts, trying a few common families before
    ///     falling back to the platform default. Returns null if the platform exposes no fonts at all.
    /// </summary>
    static SKTypeface? ResolveTypeface()
    {
        string[] preferred = { "Arial Black", "Arial", "Helvetica", "DejaVu Sans", "Liberation Sans", "Noto Sans" };
        foreach (var family in preferred)
        {
            var match = SKTypeface.FromFamilyName(family, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
            // FromFamilyName never returns null, but falls back to the default when the family is missing;
            // accept the first that resolves to a real (named) face, otherwise keep looking.
            if (match != null && !string.IsNullOrEmpty(match.FamilyName) && match.FamilyName.Equals(family, StringComparison.OrdinalIgnoreCase))
                return match;
            match?.Dispose();
        }

        var fallback = SKTypeface.FromFamilyName(null, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                       ?? SKTypeface.Default;
        // A usable default still has a family name on any host with at least one font.
        return string.IsNullOrEmpty(fallback?.FamilyName) ? null : fallback;
    }
}
