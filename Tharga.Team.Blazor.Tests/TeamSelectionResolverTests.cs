using Moq;
using Tharga.Team;
using Tharga.Team.Blazor.Features.Team;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Tests for <see cref="TeamSelectionResolver"/> — the rule that keeps cross-team visibility from
/// turning into cross-team occupancy. A team the caller *chose* (this session, or remembered from a
/// previous visit) is honoured even without membership; a team they never chose is never defaulted to.
/// </summary>
public class TeamSelectionResolverTests
{
    private static ITeam Team(string key) => Mock.Of<ITeam>(t => t.Key == key);

    private static IReadOnlyList<ITeam> Teams(params string[] keys) => keys.Select(Team).ToArray();

    [Fact]
    public void Resolve_HonoursTheCurrentSelectionOfAVisibleTeam()
    {
        var result = TeamSelectionResolver.Resolve("other", null, Teams("mine", "other"), Teams("mine"));

        Assert.Equal("other", result.Key);
    }

    [Fact]
    public void Resolve_RestoresARememberedNonMemberTeam()
    {
        var result = TeamSelectionResolver.Resolve(null, "customer-a", Teams("customer-a", "mine"), Teams("mine"));

        Assert.Equal("customer-a", result.Key);
    }

    [Fact]
    public void Resolve_CurrentSelectionWinsOverTheRememberedOne()
    {
        var result = TeamSelectionResolver.Resolve("customer-b", "customer-a", Teams("customer-a", "customer-b"), Teams());

        Assert.Equal("customer-b", result.Key);
    }

    /// <summary>
    /// The core guard: with nothing chosen, the fallback comes from own memberships — never from the
    /// widened visible set.
    /// </summary>
    [Fact]
    public void Resolve_NothingChosen_FallsBackToOwnTeams()
    {
        var result = TeamSelectionResolver.Resolve(null, null, Teams("customer-a", "customer-b", "mine"), Teams("mine"));

        Assert.Equal("mine", result.Key);
    }

    /// <summary>
    /// A support engineer with the scope but no memberships and no prior choice must land nowhere,
    /// rather than inside the first tenant in the list.
    /// </summary>
    [Fact]
    public void Resolve_NoOwnTeamsAndNothingChosen_SelectsNothing()
    {
        var result = TeamSelectionResolver.Resolve(null, null, Teams("customer-a", "customer-b"), Teams());

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_NoOwnTeams_StillRestoresARememberedTeam()
    {
        var result = TeamSelectionResolver.Resolve(null, "customer-b", Teams("customer-a", "customer-b"), Teams());

        Assert.Equal("customer-b", result.Key);
    }

    /// <summary>
    /// A remembered team the caller can no longer see — access revoked, team deleted, scope removed —
    /// must not resurrect it.
    /// </summary>
    [Fact]
    public void Resolve_RememberedTeamNoLongerVisible_FallsBackToOwnTeams()
    {
        var result = TeamSelectionResolver.Resolve(null, "customer-a", Teams("mine"), Teams("mine"));

        Assert.Equal("mine", result.Key);
    }

    [Fact]
    public void Resolve_RememberedTeamNoLongerVisibleAndNoOwnTeams_SelectsNothing()
    {
        var result = TeamSelectionResolver.Resolve(null, "customer-a", Teams(), Teams());

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_UnknownCurrentSelection_FallsBackThroughRemembered()
    {
        var result = TeamSelectionResolver.Resolve("deleted", "customer-a", Teams("customer-a", "mine"), Teams("mine"));

        Assert.Equal("customer-a", result.Key);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    public void Resolve_NoKeys_FallsBackToOwnTeams(string currentTeamKey, string rememberedTeamKey)
    {
        var result = TeamSelectionResolver.Resolve(currentTeamKey, rememberedTeamKey, Teams("customer-a", "mine"), Teams("mine"));

        Assert.Equal("mine", result.Key);
    }

    [Fact]
    public void Resolve_NullCollections_DoNotThrow()
    {
        Assert.Null(TeamSelectionResolver.Resolve("any", "other", null, null));
    }
}
