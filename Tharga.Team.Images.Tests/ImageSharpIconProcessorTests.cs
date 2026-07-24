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

    [Fact]
    public async Task Oversized_IsDownscaledToFit_PreservingAspect()
    {
        var data = PngOf(1000, 500);
        var result = await Build(256).ProcessAsync(data, "image/png");

        using var image = Image.Load(result.Data);
        Assert.Equal("image/png", result.ContentType);
        Assert.True(image.Width <= 256 && image.Height <= 256);
        Assert.Equal(256, image.Width);   // longest side clamps to 256
        Assert.Equal(128, image.Height);  // aspect 2:1 preserved
    }

    [Fact]
    public async Task WithinBounds_IsUnchanged()
    {
        var data = PngOf(100, 80);
        var result = await Build(256).ProcessAsync(data, "image/png");
        Assert.Same(data, result.Data);
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
