namespace Tharga.Team;

/// <summary>
/// Resolves the image to display for a subject by running the registered <see cref="IIconSource"/>s in
/// order and returning the first non-null result. Returns null when no source supplies an image — the
/// caller (e.g. an avatar component) then renders an initials/default fallback via <see cref="IconInitials"/>.
/// Built-in; consumers customize resolution by registering sources, not by replacing the resolver.
/// </summary>
public interface IIconResolver
{
    Task<IconImage> ResolveAsync(IconSubject subject, CancellationToken cancellationToken = default);
}
