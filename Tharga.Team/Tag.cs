namespace Tharga.Team;

/// <summary>
/// A system-set key-value tag on an API key. Tags are a list (not a map), so the same
/// <see cref="Key"/> may appear more than once (e.g. a combination key tagged with multiple
/// types). Each tag is surfaced as a <c>tag.{Key}</c> claim on the authenticated principal.
/// </summary>
public record Tag(string Key, string Value);
