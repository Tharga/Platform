using Microsoft.Extensions.DependencyInjection;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Tests for the standard text-provider contract (Tharga/Platform#101): the default English behavior,
/// DI registration + consumer override, and the <see cref="TeamMenuText"/> key catalog.
/// </summary>
public class ThargaTextProviderTests
{
    [Fact]
    public void Default_Returns_KeyDefault()
    {
        var sut = new DefaultThargaTextProvider();

        Assert.Equal("Team", sut.Get(new TextKey("team.menu.team", "Team")));
        Assert.Equal("Anything", sut.Get(new TextKey("some.unmapped.key", "Anything")));
    }

    [Fact]
    public void AddThargaTeamBlazor_Registers_Default_TextProvider()
    {
        var services = new ServiceCollection();

        services.AddThargaTeamBlazor();

        var provider = services.BuildServiceProvider();
        Assert.IsType<DefaultThargaTextProvider>(provider.GetRequiredService<IThargaTextProvider>());
    }

    [Fact]
    public void Consumer_TextProvider_Overrides_Default_RegisteredAfter()
    {
        var services = new ServiceCollection();

        services.AddThargaTeamBlazor();
        services.AddSingleton<IThargaTextProvider, SwedishMenuText>();

        var provider = services.BuildServiceProvider();
        var text = provider.GetRequiredService<IThargaTextProvider>();

        Assert.IsType<SwedishMenuText>(text);
        Assert.Equal("Lag", text.Get(TeamMenuText.Team));
        Assert.Equal("Logout", text.Get(TeamMenuText.Logout)); // untranslated keys fall back to the English default
    }

    [Fact]
    public void Consumer_TextProvider_Overrides_Default_RegisteredBefore()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IThargaTextProvider, SwedishMenuText>();
        services.AddThargaTeamBlazor();

        var provider = services.BuildServiceProvider();
        Assert.IsType<SwedishMenuText>(provider.GetRequiredService<IThargaTextProvider>());
    }

    [Theory]
    [InlineData("team.menu.user", "User")]
    [InlineData("team.menu.team", "Team")]
    [InlineData("team.menu.logout", "Logout")]
    [InlineData("team.menu.login", "Login")]
    [InlineData("team.menu.createTeam", "Create Team")]
    [InlineData("team.menu.loading", "Loading...")]
    public void TeamMenuText_Catalog_HasExpectedKeyAndDefault(string key, string expectedDefault)
    {
        var all = new[]
        {
            TeamMenuText.User, TeamMenuText.Team, TeamMenuText.Logout,
            TeamMenuText.Login, TeamMenuText.CreateTeam, TeamMenuText.Loading
        };

        var entry = all.Single(k => k.Key == key);
        Assert.Equal(expectedDefault, entry.Default);
    }

    private sealed class SwedishMenuText : IThargaTextProvider
    {
        public string Get(TextKey key) => key.Key == TeamMenuText.Team.Key ? "Lag" : key.Default;
    }
}
