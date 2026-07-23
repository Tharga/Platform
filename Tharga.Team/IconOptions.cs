namespace Tharga.Team;

/// <summary>
/// Limits applied when accepting an icon (upload or downloaded URL). Configured on the platform options
/// and enforced by <see cref="IconValidation"/>.
/// </summary>
public class IconOptions
{
    /// <summary>Maximum accepted icon size in bytes. Default 256 KB.</summary>
    public int MaxBytes { get; set; } = 256 * 1024;

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
