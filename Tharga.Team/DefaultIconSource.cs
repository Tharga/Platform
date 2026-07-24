namespace Tharga.Team;

/// <summary>
/// Built-in <see cref="IIconSource"/> that resolves a <b>user</b> subject to a configured generic default
/// image (<see cref="IconSettings.DefaultUserIconUrl"/>), when set. Registered after
/// <see cref="GravatarIconSource"/>, so it applies when Gravatar is disabled or produced nothing. Returns
/// null when no default is configured (the avatar then falls back to initials).
/// </summary>
public sealed class DefaultIconSource : IIconSource
{
    private readonly IconSettings _settings;

    public DefaultIconSource(IconSettings settings = null)
    {
        _settings = settings ?? new IconSettings();
    }

    public Task<IconImage> ResolveAsync(IconSubject subject, CancellationToken cancellationToken = default)
    {
        if (subject?.Kind != IconKind.User || string.IsNullOrWhiteSpace(_settings.DefaultUserIconUrl))
            return Task.FromResult<IconImage>(null);

        return Task.FromResult(new IconImage(_settings.DefaultUserIconUrl));
    }
}
