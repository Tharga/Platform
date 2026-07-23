using System.Security.Cryptography;
using System.Text;

namespace Tharga.Team;

/// <summary>
/// Built-in <see cref="IIconSource"/> that resolves a <b>user</b> subject with an email to its Gravatar
/// image. Registered after <see cref="StoredIconSource"/> and any consumer sources, so an explicitly
/// uploaded user icon (and custom sources) take precedence and Gravatar is the fallback. Returns null for
/// team subjects or users without an email.
/// </summary>
public sealed class GravatarIconSource : IIconSource
{
    private const string DefaultStyle = "identicon";

    public Task<IconImage> ResolveAsync(IconSubject subject, CancellationToken cancellationToken = default)
    {
        if (subject?.Kind != IconKind.User || string.IsNullOrWhiteSpace(subject.EMail))
            return Task.FromResult<IconImage>(null);

        var hash = Md5Hex(subject.EMail.Trim().ToLowerInvariant());
        return Task.FromResult(new IconImage($"https://www.gravatar.com/avatar/{hash}?d={DefaultStyle}"));
    }

    private static string Md5Hex(string value)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(value));
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) builder.Append(b.ToString("x2"));
        return builder.ToString();
    }
}
