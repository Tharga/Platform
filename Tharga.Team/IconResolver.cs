namespace Tharga.Team;

/// <summary>
/// Default <see cref="IIconResolver"/>: returns the first non-null image from the registered
/// <see cref="IIconSource"/>s, in registration order. The built-in <see cref="StoredIconSource"/> is
/// registered first, so an explicitly-set (platform-stored) icon takes precedence; consumer sources run
/// after it and fill in only when no icon has been set.
/// </summary>
public sealed class IconResolver : IIconResolver
{
    private readonly IReadOnlyList<IIconSource> _sources;

    public IconResolver(IEnumerable<IIconSource> sources)
    {
        _sources = sources?.ToList() ?? [];
    }

    public async Task<IconImage> ResolveAsync(IconSubject subject, CancellationToken cancellationToken = default)
    {
        if (subject == null) return null;

        foreach (var source in _sources)
        {
            var image = await source.ResolveAsync(subject, cancellationToken);
            if (image != null) return image;
        }

        return null;
    }
}
