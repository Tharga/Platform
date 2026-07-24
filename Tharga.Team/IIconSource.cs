namespace Tharga.Team;

/// <summary>
/// A source of icon images. This is the <b>sourcing</b> seam — where a displayed image <i>comes from</i>,
/// separate from where bytes are stored (<see cref="IIconStore"/>). The platform runs the built-in
/// <see cref="StoredIconSource"/> first (an explicitly-set icon wins), then the registered sources (via
/// <c>o.AddIconSource&lt;T&gt;()</c>) in order, so a consuming system can supply icons for subjects that
/// have no platform-stored icon — returning null to defer to the next source.
/// </summary>
public interface IIconSource
{
    /// <summary>
    /// Resolve an image for <paramref name="subject"/>, or null to defer to the next source.
    /// </summary>
    Task<IconImage> ResolveAsync(IconSubject subject, CancellationToken cancellationToken = default);
}
