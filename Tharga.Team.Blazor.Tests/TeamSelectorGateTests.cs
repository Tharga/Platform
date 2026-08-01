using Tharga.Team.Blazor.Features.Team;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Tests for <see cref="TeamSelectorGate"/> — what the team selector offers a caller who belongs to no
/// team. Pure-function tests to match the other gating tests in this project (no bUnit, so razor markup
/// cannot be asserted directly).
/// </summary>
public class TeamSelectorGateTests
{
    /// <summary>The ordinary case the link exists for: a new user with no teams and creation allowed.</summary>
    [Fact]
    public void ShowCreateTeamLink_NoTeamsAndCreationAllowed_IsShown()
    {
        Assert.True(TeamSelectorGate.ShowCreateTeamLink(0, true));
    }

    /// <summary>
    /// The defect this closes. A host setting <c>AllowTeamCreation = false</c> still saw a "Create team"
    /// link, and following it reached an operation the service layer refuses — team creation has required
    /// the option at the service since 3.1.2.
    /// </summary>
    [Fact]
    public void ShowCreateTeamLink_CreationDisabled_IsHidden()
    {
        Assert.False(TeamSelectorGate.ShowCreateTeamLink(0, false));
    }

    /// <summary>
    /// The link belongs to the teamless branch only. A caller who already has teams gets the selector
    /// itself, so creation must not appear here regardless of the option.
    /// </summary>
    [Theory]
    [InlineData(1, true)]
    [InlineData(1, false)]
    [InlineData(5, true)]
    [InlineData(5, false)]
    public void ShowCreateTeamLink_CallerHasTeams_IsHidden(int teamCount, bool allowTeamCreation)
    {
        Assert.False(TeamSelectorGate.ShowCreateTeamLink(teamCount, allowTeamCreation));
    }

    /// <summary>
    /// Agrees with <c>TeamComponent</c>, which has always read the option. The two surfaces contradicting
    /// each other is what made this a defect rather than a missing feature, so the agreement is the thing
    /// worth pinning.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShowCreateTeamLink_MatchesTeamComponentForATeamlessCaller(bool allowTeamCreation)
    {
        // TeamComponent renders its "Create new Team" button on `!_teams.Any() && _allowTeamCreation`.
        var teamComponentShowsButton = allowTeamCreation;

        Assert.Equal(teamComponentShowsButton, TeamSelectorGate.ShowCreateTeamLink(0, allowTeamCreation));
    }
}
