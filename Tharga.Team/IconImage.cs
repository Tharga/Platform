namespace Tharga.Team;

/// <summary>
/// A resolved icon image to render — a URL, whether the platform's own serving endpoint or an address a
/// custom <see cref="IIconSource"/> supplied.
/// </summary>
/// <param name="Url">The image URL.</param>
public sealed record IconImage(string Url);
