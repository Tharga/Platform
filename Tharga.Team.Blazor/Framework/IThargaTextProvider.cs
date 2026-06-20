namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// Resolves UI strings for Tharga.Team components. Register a custom implementation to localize the
/// strings — e.g. by bridging to an application content/localization system — and it overrides the
/// built-in <see cref="DefaultThargaTextProvider"/>, which returns each key's English default.
/// </summary>
public interface IThargaTextProvider
{
    /// <summary>
    /// Returns the localized string for <paramref name="key"/>, or its English default
    /// (<see cref="TextKey.Default"/>) when no translation is available.
    /// </summary>
    string Get(TextKey key);
}
