using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using Tharga.Team;

namespace Tharga.Team.Images;

/// <summary>
/// <see cref="IIconProcessor"/> that downscales an uploaded raster image to fit within
/// <see cref="IconOptions.MaxDimension"/> (aspect preserved, never upscaled), re-encoding as PNG. Images
/// already within the box, and formats ImageSharp cannot load (e.g. SVG), pass through unchanged.
/// </summary>
public sealed class ImageSharpIconProcessor : IIconProcessor
{
    private readonly IconOptions _options;

    public ImageSharpIconProcessor(IOptions<IconOptions> options = null)
    {
        _options = options?.Value ?? new IconOptions();
    }

    public async Task<IconContent> ProcessAsync(byte[] data, string contentType, CancellationToken cancellationToken = default)
    {
        var max = _options.MaxDimension;
        if (max <= 0 || data == null || data.Length == 0)
            return new IconContent(data, contentType);

        try
        {
            using var image = Image.Load(data);
            if (image.Width <= max && image.Height <= max)
                return new IconContent(data, contentType);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(max, max)
            }));

            using var stream = new MemoryStream();
            await image.SaveAsync(stream, new PngEncoder(), cancellationToken);
            return new IconContent(stream.ToArray(), "image/png");
        }
        catch
        {
            // Not a raster image ImageSharp can decode (e.g. SVG) — leave it untouched.
            return new IconContent(data, contentType);
        }
    }
}
