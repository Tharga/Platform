using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using Tharga.Team;
using Tharga.Team.Images;

namespace Tharga.Team.Images.Tests;

/// <summary>
/// <see cref="ImageSharpIconProcessor"/>: downscales oversized raster images to fit the max dimension
/// (aspect preserved, PNG output), leaves within-bounds images and non-decodable data untouched, and
/// respects a disabled (0) max dimension.
/// </summary>
public class ImageSharpIconProcessorTests
{
    private static ImageSharpIconProcessor Build(int maxDimension = 256)
        => new(Options.Create(new IconOptions { MaxDimension = maxDimension }));

    private static byte[] PngOf(int width, int height)
    {
        using var image = new Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(width, height);
        using var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        return stream.ToArray();
    }

    /// <summary>Fully opaque, so padded pixels are distinguishable from original ones by alpha alone.</summary>
    private static byte[] OpaquePngOf(int width, int height)
    {
        using var image = new Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(width, height,
            new SixLabors.ImageSharp.PixelFormats.Rgba32(10, 20, 30, 255));
        using var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        return stream.ToArray();
    }

    /// <summary>
    /// Downscaled to fit, then padded to square. Before squaring shipped this returned 256x128, and
    /// every avatar surface letterboxed it differently.
    /// </summary>
    [Fact]
    public async Task Oversized_IsDownscaledThenPaddedToSquare()
    {
        var data = PngOf(1000, 500);
        var result = await Build(256).ProcessAsync(data, "image/png");

        using var image = Image.Load(result.Data);
        Assert.Equal("image/png", result.ContentType);
        Assert.Equal(256, image.Width);
        Assert.Equal(256, image.Height);
    }

    /// <summary>
    /// Within bounds is no longer sufficient to skip — it has to be square too. Squares to the long side
    /// (100), not to MaxDimension (256), so no pixel is invented.
    /// </summary>
    [Fact]
    public async Task WithinBoundsButNotSquare_IsPaddedWithoutUpscaling()
    {
        var data = PngOf(100, 50);
        var result = await Build(256).ProcessAsync(data, "image/png");

        using var image = Image.Load(result.Data);
        Assert.Equal(100, image.Width);
        Assert.Equal(100, image.Height);
    }

    /// <summary>Tall sources take the same path as wide ones.</summary>
    [Fact]
    public async Task TallWithinBounds_IsPaddedToTheLongSide()
    {
        var data = PngOf(50, 100);
        var result = await Build(256).ProcessAsync(data, "image/png");

        using var image = Image.Load(result.Data);
        Assert.Equal(100, image.Width);
        Assert.Equal(100, image.Height);
    }

    /// <summary>Square and within bounds is the one case that still passes through untouched.</summary>
    [Fact]
    public async Task SquareAndWithinBounds_IsUnchanged()
    {
        var data = PngOf(100, 100);
        var result = await Build(256).ProcessAsync(data, "image/png");
        Assert.Same(data, result.Data);
    }

    /// <summary>Square but oversized still downscales — squaring did not make it a no-op.</summary>
    [Fact]
    public async Task SquareButOversized_IsDownscaled()
    {
        var data = PngOf(300, 300);
        var result = await Build(256).ProcessAsync(data, "image/png");

        using var image = Image.Load(result.Data);
        Assert.Equal(256, image.Width);
        Assert.Equal(256, image.Height);
    }

    /// <summary>
    /// The padding is transparent, not black. This is why the image is loaded as Rgba32 regardless of
    /// source format — a JPEG decodes without an alpha channel, and padding that with a transparent
    /// colour produces black bars.
    /// </summary>
    [Fact]
    public async Task Padding_IsTransparent()
    {
        var data = OpaquePngOf(100, 50);
        var result = await Build(256).ProcessAsync(data, "image/png");

        using var image = Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(result.Data);
        Assert.Equal(0, image[50, 5].A);    // padded band at the top
        Assert.Equal(0, image[50, 95].A);   // padded band at the bottom
        Assert.Equal(255, image[50, 50].A); // original content in the middle
    }

    /// <summary>Content is never cropped — the source pixels survive squaring.</summary>
    [Fact]
    public async Task Padding_DoesNotCropContent()
    {
        var data = OpaquePngOf(100, 50);
        var result = await Build(256).ProcessAsync(data, "image/png");

        using var image = Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(result.Data);
        Assert.Equal(255, image[0, 50].A);  // left edge of the original row
        Assert.Equal(255, image[99, 50].A); // right edge of the original row
    }

    [Fact]
    public async Task MaxDimensionZero_Disabled_PassesThrough()
    {
        var data = PngOf(1000, 1000);
        var result = await Build(0).ProcessAsync(data, "image/png");
        Assert.Same(data, result.Data);
    }

    [Fact]
    public async Task NonImageData_PassesThroughUnchanged()
    {
        var data = new byte[] { 1, 2, 3, 4 };
        var result = await Build(256).ProcessAsync(data, "image/svg+xml");
        Assert.Same(data, result.Data);
        Assert.Equal("image/svg+xml", result.ContentType);
    }
}
