namespace Tharga.Team;

/// <summary>
/// Default <see cref="IIconResolver"/>: returns the first non-null image from the registered
/// <see cref="IIconSource"/>s, in registration order. Consumer sources are registered ahead of the
/// built-in <see cref="StoredIconSource"/>, so a custom source may override — or, by returning null,
/// defer to — an explicitly-set icon.
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
