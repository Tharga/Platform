using Tharga.Team.Blazor.Framework;

namespace Tharga.Platform.Sample.Framework;

/// <summary>
/// Demonstrates localizing the Tharga.Team menu strings via <see cref="IThargaTextProvider"/> — here a
/// small in-memory Swedish dictionary keyed by <see cref="TextKey.Key"/>. A real app would delegate to
/// its own content/localization system instead. Keys not in the map fall back to the bundled English
/// default (<see cref="TextKey.Default"/>).
/// </summary>
public sealed class SampleMenuTextProvider : IThargaTextProvider
{
    private static readonly IReadOnlyDictionary<string, string> Translations = new Dictionary<string, string>
    {
        [TeamMenuText.User.Key] = "Användare",
        [TeamMenuText.Team.Key] = "Lag",
        [TeamMenuText.Logout.Key] = "Logga ut",
        [TeamMenuText.Login.Key] = "Logga in",
        [TeamMenuText.CreateTeam.Key] = "Skapa lag",
        [TeamMenuText.Loading.Key] = "Laddar…",
    };

    public Task<string> GetAsync(TextKey key) =>
        Task.FromResult(Translations.TryGetValue(key.Key, out var value) ? value : key.Default);
}
