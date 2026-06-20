namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// Default <see cref="IThargaTextProvider"/> — returns each key's English default. Registered
/// automatically by <c>AddThargaTeamBlazor</c>; a consumer-registered provider overrides it.
/// </summary>
internal sealed class DefaultThargaTextProvider : IThargaTextProvider
{
    public Task<string> GetAsync(TextKey key) => Task.FromResult(key.Default);
}
