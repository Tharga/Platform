namespace Tharga.Team;

/// <summary>
/// A source of icon images. This is the <b>sourcing</b> seam — where a displayed image <i>comes from</i>,
/// separate from where bytes are stored (<see cref="IIconStore"/>). The platform composes the registered
/// sources (via <c>o.AddIconSource&lt;T&gt;()</c>) ahead of the built-in <see cref="StoredIconSource"/>, so
/// a consuming system can supply icons from its own place — returning null to defer to the next source.
/// </summary>
public interface IIconSource
{
    /// <summary>
    /// Resolve an image for <paramref name="subject"/>, or null to defer to the next source.
    /// </summary>
    Task<IconImage> ResolveAsync(IconSubject subject, CancellationToken cancellationToken = default);
}
