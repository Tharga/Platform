using System.Security.Cryptography;
using System.Text;

namespace Tharga.Team;

/// <summary>
/// Built-in <see cref="IIconSource"/> that resolves a <b>user</b> subject with an email to its Gravatar
/// image, when enabled via <see cref="IconSettings"/>. Registered after <see cref="StoredIconSource"/> and
/// any consumer sources, so an explicitly uploaded user icon (and custom sources) take precedence and
/// Gravatar is a fallback. Returns null for team subjects, users without an email, or when disabled.
/// </summary>
public sealed class GravatarIconSource : IIconSource
{
    private readonly IconSettings _settings;

    public GravatarIconSource(IconSettings settings = null)
    {
        _settings = settings ?? new IconSettings();
    }

    public Task<IconImage> ResolveAsync(IconSubject subject, CancellationToken cancellationToken = default)
    {
        if (!_settings.GravatarEnabled || subject?.Kind != IconKind.User || string.IsNullOrWhiteSpace(subject.EMail))
            return Task.FromResult<IconImage>(null);

        var style = string.IsNullOrWhiteSpace(_settings.GravatarStyle) ? "identicon" : _settings.GravatarStyle;
        var hash = Md5Hex(subject.EMail.Trim().ToLowerInvariant());
        return Task.FromResult(new IconImage($"https://www.gravatar.com/avatar/{hash}?d={style}"));
    }

    private static string Md5Hex(string value)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(value));
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) builder.Append(b.ToString("x2"));
        return builder.ToString();
    }
}
