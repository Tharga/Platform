using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tharga.Team;

namespace Tharga.Team.Images;

/// <summary>
/// <see cref="IIconProcessor"/> that squares an uploaded raster image and fits it within
/// <see cref="IconOptions.MaxDimension"/>, re-encoding as PNG. The short side is extended with
/// transparent padding — never cropped — so avatar surfaces that reserve a square box no longer
/// letterbox wide or tall sources inconsistently. Images that are already square and within the box, and
/// formats ImageSharp cannot load (e.g. SVG), pass through unchanged.
/// </summary>
/// <remarks>
/// <b>Content is never upscaled and never cropped.</b> The output side is
/// <c>min(max(width, height), MaxDimension)</c>, so the fit-inside scale is exactly 1 whenever the source
/// already fits — a 100×50 becomes 100×100, not 256×256. Cropping would solve squaring too, and is
/// precisely the failure to avoid: it takes a face out of a portrait photo.
/// <para>
/// Loaded as <c>Rgba32</c> regardless of source format. A JPEG decodes to RGB with no alpha channel, and
/// padding that with a transparent colour yields black bars rather than transparency.
/// </para>
/// </remarks>
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
            using var image = Image.Load<Rgba32>(data);

            // "Within bounds" alone is not enough to skip: a 100x50 fits the box and is still not square.
            if (image.Width == image.Height && image.Width <= max)
                return new IconContent(data, contentType);

            var side = Math.Min(Math.Max(image.Width, image.Height), max);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Pad,
                Size = new Size(side, side),
                PadColor = Color.Transparent
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
