namespace Tharga.Team;

/// <summary>
/// The bytes and content type of a stored icon, as returned by <see cref="IIconStore.LoadAsync"/>.
/// </summary>
/// <param name="Data">The raw image bytes.</param>
/// <param name="ContentType">The image MIME type (e.g. <c>image/png</c>).</param>
public sealed record IconContent(byte[] Data, string ContentType);
