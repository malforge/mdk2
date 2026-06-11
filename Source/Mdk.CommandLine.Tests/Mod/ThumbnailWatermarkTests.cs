using System;
using System.IO;
using FakeItEasy;
using Mdk.CommandLine.Mod.Pack;
using Mdk.CommandLine.Shared.Api;
using NUnit.Framework;
using SkiaSharp;

namespace MDK.CommandLine.Tests.Mod;

[TestFixture]
public class ThumbnailWatermarkTests
{
    string _tempDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ThumbnailWatermarkTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, true);
    }

    [Test]
    public void TryStamp_ProducesAValidDifferentPng()
    {
        var source = Path.Combine(_tempDirectory, "thumb.png");
        var dest = Path.Combine(_tempDirectory, "out", "thumb.png");
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        WriteSolidPng(source, 256, 256, new SKColor(120, 120, 120));
        var console = A.Fake<IConsole>();

        var stamped = ThumbnailWatermark.TryStamp(source, dest, "ALPHA", console);

        if (!stamped)
        {
            Assert.Inconclusive("No system font available on this host to render the watermark.");
            return;
        }

        Assert.That(File.Exists(dest), Is.True);
        using var result = SKBitmap.Decode(dest);
        Assert.That(result, Is.Not.Null, "Watermarked output should be a decodable PNG.");
        Assert.That(result!.Width, Is.EqualTo(256));
        Assert.That(result.Height, Is.EqualTo(256));
        Assert.That(File.ReadAllBytes(dest), Is.Not.EqualTo(File.ReadAllBytes(source)), "Watermarked output should differ from the source.");
    }

    [Test]
    public void TryStamp_MissingSource_ReturnsFalse()
    {
        var dest = Path.Combine(_tempDirectory, "thumb.png");
        var console = A.Fake<IConsole>();

        var stamped = ThumbnailWatermark.TryStamp(Path.Combine(_tempDirectory, "does-not-exist.png"), dest, "ALPHA", console);

        Assert.That(stamped, Is.False);
        Assert.That(File.Exists(dest), Is.False);
    }

    static void WriteSolidPng(string path, int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
            canvas.Clear(color);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }
}
