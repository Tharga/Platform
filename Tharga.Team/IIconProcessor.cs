namespace Tharga.Team;

/// <summary>
/// Optional pre-processing applied to icon bytes before they are stored — e.g. downscaling an oversized
/// image so it fits the configured limits. The built-in store runs the registered processor before
/// validating/persisting. Register an implementation (e.g. <c>AddThargaImageProcessing</c> from the
/// <c>Tharga.Team.Images</c> package) to enable it; without one, icons are stored as-is.
/// </summary>
public interface IIconProcessor
{
    /// <summary>
    /// Process the image, returning the (possibly transformed) bytes and content type. Implementations
    /// should return the input unchanged when no processing applies (e.g. an unsupported format).
    /// </summary>
    Task<IconContent> ProcessAsync(byte[] data, string contentType, CancellationToken cancellationToken = default);
}
