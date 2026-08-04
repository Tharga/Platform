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

    // --- ShowCreateTeamLink: hiding the top-bar link without touching what governs creation ---

    /// <summary>
    /// The ask: creation stays reachable from the team page, but the top bar does not advertise it.
    /// Purely a layout decision, so it is the selector's own parameter rather than the shared option.
    /// </summary>
    [Fact]
    public void ShowCreateTeamLink_HiddenByTheParameter_EvenWhenCreationIsAllowed()
    {
        Assert.True(TeamSelectorGate.ShowCreateTeamLink(0, allowTeamCreation: true, showLink: true));
        Assert.False(TeamSelectorGate.ShowCreateTeamLink(0, allowTeamCreation: true, showLink: false));
    }

    /// <summary>
    /// <b>Hiding the link is not a permission.</b> The parameter suppresses an affordance; whether teams
    /// may be created is <c>AllowTeamCreation</c>, which the service enforces too. Asserted so nobody
    /// later "simplifies" the two into one flag — that would turn a layout switch into the only thing
    /// standing between a caller and an operation, which is the shape of a security bug rather than a
    /// tidy-up.
    /// </summary>
    [Fact]
    public void ShowCreateTeamLink_TheParameterCannotSubstituteForTheOption()
    {
        // Creation disabled: the link is gone whatever the parameter says.
        Assert.False(TeamSelectorGate.ShowCreateTeamLink(0, allowTeamCreation: false, showLink: true));
        Assert.False(TeamSelectorGate.ShowCreateTeamLink(0, allowTeamCreation: false, showLink: false));
    }

    /// <summary>Default true, so an existing host sees no change.</summary>
    [Fact]
    public void ShowCreateTeamLink_DefaultsToShown()
    {
        Assert.Equal(
            TeamSelectorGate.ShowCreateTeamLink(0, allowTeamCreation: true),
            TeamSelectorGate.ShowCreateTeamLink(0, allowTeamCreation: true, showLink: true));
    }

    /// <summary>
    /// The self-check: without it, every assertion above would still pass if the parameter were ignored
    /// and the result driven entirely by the other two inputs.
    /// </summary>
    [Fact]
    public void ShowCreateTeamLink_TheParameterIsWhatDecides()
    {
        // Same teamCount, same option — only the parameter differs, and it must change the answer.
        Assert.NotEqual(
            TeamSelectorGate.ShowCreateTeamLink(0, allowTeamCreation: true, showLink: true),
            TeamSelectorGate.ShowCreateTeamLink(0, allowTeamCreation: true, showLink: false));
    }
}
