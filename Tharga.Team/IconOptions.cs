namespace Tharga.Team;

/// <summary>
/// Limits applied when accepting an icon (upload or downloaded URL). Configured on the platform options
/// and enforced by <see cref="IconValidation"/>.
/// </summary>
public class IconOptions
{
    /// <summary>
    /// Maximum <b>stored</b> icon size in bytes — validated after any processing (downscaling). Default 256 KB.
    /// </summary>
    public int MaxBytes { get; set; } = 256 * 1024;

    /// <summary>
    /// Maximum size (bytes) of an original upload accepted for reading, <b>before</b> processing. Larger
    /// than <see cref="MaxBytes"/> so oversized images can be read and then downscaled (when an image
    /// processor is registered) rather than rejected outright. Default 10 MB.
    /// </summary>
    public int MaxUploadBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Maximum icon dimension (width and height, px). When an image processor is registered, larger images
    /// are downscaled to fit within this box before storing (aspect preserved, never upscaled). Default 256.
    /// 0 disables resizing.
    /// </summary>
    public int MaxDimension { get; set; } = 256;

    /// <summary>Accepted image content types. Default: png, jpeg, gif, webp, svg.</summary>
    public IReadOnlyCollection<string> AllowedContentTypes { get; set; } =
    [
        "image/png",
        "image/jpeg",
        "image/gif",
        "image/webp",
        "image/svg+xml"
    ];
}
