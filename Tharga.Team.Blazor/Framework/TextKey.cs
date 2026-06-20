namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// Identifies a localizable UI string by a stable <see cref="Key"/> and bundles its English
/// <see cref="Default"/>, so a call site can never reference a key without also supplying its fallback.
/// </summary>
/// <param name="Key">Stable lookup key, e.g. <c>"team.menu.team"</c>.</param>
/// <param name="Default">English text used when no translation is registered for the key.</param>
public readonly record struct TextKey(string Key, string Default);
