using Moq;
using Tharga.Team;
using Tharga.Team.Blazor.Features.Team;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Tests for <see cref="TeamSelectionResolver"/> — the rule that keeps cross-team visibility from
/// turning into cross-team occupancy. An oversight caller may deliberately select someone else's team,
/// but must never be *defaulted* into one.
/// </summary>
public class TeamSelectionResolverTests
{
    private static ITeam Team(string key) => Mock.Of<ITeam>(t => t.Key == key);

    private static IReadOnlyList<ITeam> Teams(params string[] keys) => keys.Select(Team).ToArray();

    [Fact]
    public void Resolve_HonoursAnExplicitSelectionOfAVisibleTeam()
    {
        var result = TeamSelectionResolver.Resolve("other", Teams("mine", "other"), Teams("mine"));

        Assert.Equal("other", result.Key);
    }

    /// <summary>
    /// The core guard: with no valid explicit selection, the fallback must come from the caller's own
    /// teams — never from the widened visible set.
    /// </summary>
    [Fact]
    public void Resolve_FallsBackToOwnTeams_NotTheWidenedSet()
    {
        var result = TeamSelectionResolver.Resolve(null, Teams("customer-a", "customer-b", "mine"), Teams("mine"));

        Assert.Equal("mine", result.Key);
    }

    [Fact]
    public void Resolve_UnknownSelection_FallsBackToOwnTeams()
    {
        var result = TeamSelectionResolver.Resolve("deleted-team", Teams("customer-a", "mine"), Teams("mine"));

        Assert.Equal("mine", result.Key);
    }

    /// <summary>
    /// A support engineer with the scope but no memberships must land nowhere rather than inside the
    /// first tenant in the list.
    /// </summary>
    [Fact]
    public void Resolve_NoOwnTeams_SelectsNothing()
    {
        var result = TeamSelectionResolver.Resolve(null, Teams("customer-a", "customer-b"), Teams());

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_NoOwnTeams_StillHonoursAnExplicitSelection()
    {
        var result = TeamSelectionResolver.Resolve("customer-b", Teams("customer-a", "customer-b"), Teams());

        Assert.Equal("customer-b", result.Key);
    }

    [Fact]
    public void Resolve_SelectionNotVisible_IsNotHonoured()
    {
        var result = TeamSelectionResolver.Resolve("hidden", Teams("mine"), Teams("mine"));

        Assert.Equal("mine", result.Key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Resolve_NoSelectionKey_FallsBackToOwnTeams(string selectedTeamKey)
    {
        var result = TeamSelectionResolver.Resolve(selectedTeamKey, Teams("customer-a"), Teams("mine"));

        Assert.Equal("mine", result.Key);
    }

    [Fact]
    public void Resolve_NullCollections_DoNotThrow()
    {
        Assert.Null(TeamSelectionResolver.Resolve("any", null, null));
    }
}
