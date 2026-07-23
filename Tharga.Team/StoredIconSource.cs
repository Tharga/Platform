namespace Tharga.Team;

/// <summary>
/// Built-in <see cref="IIconSource"/> that serves an explicitly-set icon: when the subject has an
/// <see cref="IconSubject.IconReference"/>, it resolves to that icon's serving-endpoint URL; otherwise
/// null. Registered first, ahead of any consumer sources, so a platform-stored icon takes precedence.
/// </summary>
public sealed class StoredIconSource : IIconSource
{
    public Task<IconImage> ResolveAsync(IconSubject subject, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(subject?.IconReference))
            return Task.FromResult<IconImage>(null);

        return Task.FromResult(new IconImage(IconRoute.Url(subject.IconReference)));
    }
}
