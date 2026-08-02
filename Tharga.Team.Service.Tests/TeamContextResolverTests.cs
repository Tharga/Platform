using System.Security.Claims;
using Tharga.Team;
using Tharga.Team.Service;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Which team a request acts on, decided from the credential and a header — never from a parameter.
/// </summary>
/// <remarks>
/// A team API key is bound to one team and can be nothing else, so a team parameter beside it would be a
/// second source of truth for one question. They can disagree, and an API shaped to allow that is wrong
/// even though the disagreement is refused — <c>TeamKeyConfinementTests</c> proves it is. The check is
/// right; the parameter should not have existed to need it.
/// </remarks>
public class TeamContextResolverTests
{
    private const string OwnTeam = "team-1";
    private const string OtherTeam = "team-2";

    private sealed record FakeTeam(string Key, string[] ConsentedRoles, AccessLevel? ConsentAccessLevel) : ITeam
    {
        public string Name => Key;
        public string Icon => null;
    }

    private static ClaimsPrincipal TeamKeyCaller(string teamKey)
        => new(new ClaimsIdentity([new Claim(TeamClaimTypes.TeamKey, teamKey)], "Test"));

    private static ClaimsPrincipal SystemKeyCaller()
        => new(new ClaimsIdentity([new Claim(TeamClaimTypes.IsSystemKey, "true")], "Test"));

    private static TeamContextResolver Build(ITeam team = null, params string[] scopesAtLevel)
    {
        var teamService = Substitute.For<ITeamService>();
        teamService.GetTeamByKeyAsync(Arg.Any<string>()).Returns((ITeam)null);
        if (team != null) teamService.GetTeamByKeyAsync(team.Key).Returns(team);

        var registry = Substitute.For<IScopeRegistry>();
        registry.GetEffectiveScopes(Arg.Any<AccessLevel>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
            .Returns(scopesAtLevel.Length == 0 ? ["team:read"] : scopesAtLevel);

        return new TeamContextResolver(teamService, registry);
    }

    // ---------------- a team key ----------------

    [Fact]
    public async Task ATeamKey_ActsOnItsOwnTeam_WithNoHeader()
    {
        var context = await Build().ResolveAsync(TeamKeyCaller(OwnTeam), headerTeamKey: null);

        Assert.False(context.IsRefused);
        Assert.Equal(OwnTeam, context.TeamKey);
        Assert.Null(context.Scopes);   // already on its claims; nothing granted here
    }

    /// <summary>A header naming its own team is consistent, not a contradiction.</summary>
    [Fact]
    public async Task ATeamKey_MayNameItsOwnTeam()
    {
        var context = await Build().ResolveAsync(TeamKeyCaller(OwnTeam), OwnTeam);

        Assert.False(context.IsRefused);
        Assert.Equal(OwnTeam, context.TeamKey);
    }

    /// <summary>
    /// <b>Refused, not ignored.</b> The request has said two incompatible things; answering as though the
    /// header were absent would leave the caller believing they asked for something they did not get.
    /// </summary>
    [Fact]
    public async Task ATeamKey_NamingAnotherTeam_IsRefused()
    {
        var context = await Build().ResolveAsync(TeamKeyCaller(OwnTeam), OtherTeam);

        Assert.True(context.IsRefused);
        Assert.Equal(TeamContextRefusal.Contradiction, context.Refusal);
    }

    [Fact]
    public async Task ATeamKey_NamingItsOwnTeamInDifferentCase_IsNotAContradiction()
    {
        var context = await Build().ResolveAsync(TeamKeyCaller(OwnTeam), OwnTeam.ToUpperInvariant());

        Assert.False(context.IsRefused);
    }

    // ---------------- a system key ----------------

    /// <summary>No header, no team. A system caller operates on what its system grants authorize.</summary>
    [Fact]
    public async Task ASystemKey_WithNoHeader_HasNoTeamContext()
    {
        var context = await Build().ResolveAsync(SystemKeyCaller(), headerTeamKey: null);

        Assert.False(context.IsRefused);
        Assert.Null(context.TeamKey);
    }

    /// <summary>
    /// The consented <i>level</i> is the grant — a key holds no roles, so the question is whether the
    /// team consented at all, and at what level.
    /// </summary>
    [Fact]
    public async Task ASystemKey_NamingAConsentingTeam_GetsThatLevelsScopes()
    {
        var team = new FakeTeam(OtherTeam, ["Support"], AccessLevel.User);
        var resolver = Build(team, "team:read", "member:manage");

        var context = await resolver.ResolveAsync(SystemKeyCaller(), OtherTeam);

        Assert.False(context.IsRefused);
        Assert.Equal(OtherTeam, context.TeamKey);
        Assert.Equal(["team:read", "member:manage"], context.Scopes);
    }

    /// <summary>A team that named no roles has consented to nothing, whatever level is recorded.</summary>
    [Fact]
    public async Task ASystemKey_NamingATeamThatHasNotConsented_IsRefused()
    {
        var team = new FakeTeam(OtherTeam, [], AccessLevel.Administrator);
        var context = await Build(team).ResolveAsync(SystemKeyCaller(), OtherTeam);

        Assert.True(context.IsRefused);
        Assert.Equal(TeamContextRefusal.NotConsented, context.Refusal);
    }

    /// <summary>
    /// An unknown team is refused the same way a non-consenting one is: answering differently would tell
    /// a caller which team keys are real.
    /// </summary>
    [Fact]
    public async Task ASystemKey_NamingAnUnknownTeam_IsRefusedIdentically()
    {
        var context = await Build().ResolveAsync(SystemKeyCaller(), "no-such-team");

        Assert.True(context.IsRefused);
        Assert.Equal(TeamContextRefusal.NotConsented, context.Refusal);
    }

    /// <summary>
    /// The level decides what the key gets, so a lower consent yields less. Asserted through the registry
    /// rather than by comparing levels, because which scopes a level carries is the registry's to say and
    /// a host can change it.
    /// </summary>
    [Theory]
    [InlineData(AccessLevel.Viewer)]
    [InlineData(AccessLevel.User)]
    [InlineData(AccessLevel.Administrator)]
    public async Task TheConsentedLevel_IsWhatIsAskedOfTheRegistry(AccessLevel level)
    {
        var team = new FakeTeam(OtherTeam, ["Support"], level);
        var teamService = Substitute.For<ITeamService>();
        teamService.GetTeamByKeyAsync(OtherTeam).Returns(team);

        var registry = Substitute.For<IScopeRegistry>();
        registry.GetEffectiveScopes(Arg.Any<AccessLevel>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
            .Returns(["team:read"]);

        await new TeamContextResolver(teamService, registry).ResolveAsync(SystemKeyCaller(), OtherTeam);

        registry.Received(1).GetEffectiveScopes(level, Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>());
    }
}
