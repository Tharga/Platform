using Tharga.Team.Blazor.Features.Team;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Tests for the per-team action gates (Tharga/Platform#125). The <c>team:manage</c> scope is
/// emitted only for the currently-selected team, so a global scope flag must never authorize an
/// action on a different team. Pure-function tests to match the other gating tests in this project
/// (no bUnit here, so razor markup cannot be asserted directly).
/// </summary>
public class TeamActionGateTests
{
    [Theory]
    // Scope held and the team is the selected one.
    [InlineData(true, "team-a", "team-a", true)]
    // Scope held but a different team is selected — the claim does not cover it.
    [InlineData(true, "team-a", "team-b", false)]
    // No scope, regardless of selection.
    [InlineData(false, "team-a", "team-a", false)]
    [InlineData(false, "team-a", "team-b", false)]
    // Nothing selected yet.
    [InlineData(true, null, "team-a", false)]
    // Never authorize on a null or empty team key, even if both sides "match".
    [InlineData(true, null, null, false)]
    [InlineData(true, "", "", false)]
    public void CanManage_RequiresScopeAndTheTeamToBeSelected(bool hasManageScope, string selectedTeamKey, string teamKey, bool expected)
    {
        Assert.Equal(expected, TeamActionGate.CanManage(hasManageScope, selectedTeamKey, teamKey));
    }

    [Fact]
    public void CanManage_IsCaseSensitive_OnTeamKey()
    {
        Assert.False(TeamActionGate.CanManage(true, "Team-A", "team-a"));
    }

    [Theory]
    [InlineData(true, "team-a", "team-a", true)]
    [InlineData(true, "team-a", "team-b", false)]
    [InlineData(false, "team-a", "team-a", false)]
    public void CanRename_MatchesCanManage(bool hasManageScope, string selectedTeamKey, string teamKey, bool expected)
    {
        Assert.Equal(expected, TeamActionGate.CanRename(hasManageScope, selectedTeamKey, teamKey));
    }

    [Theory]
    // All four conditions hold.
    [InlineData(true, "team-a", "team-a", true, true, true)]
    // Selected-team gate fails — this is the leak reported in #125.
    [InlineData(true, "team-a", "team-b", true, true, false)]
    // Missing scope.
    [InlineData(false, "team-a", "team-a", true, true, false)]
    // Team creation disabled.
    [InlineData(true, "team-a", "team-a", false, true, false)]
    // Not the owner.
    [InlineData(true, "team-a", "team-a", true, false, false)]
    public void CanDelete_RequiresSelectedTeamManageAndCreationAndOwner(bool hasManageScope, string selectedTeamKey, string teamKey, bool allowTeamCreation, bool isOwner, bool expected)
    {
        Assert.Equal(expected, TeamActionGate.CanDelete(hasManageScope, selectedTeamKey, teamKey, allowTeamCreation, isOwner));
    }

    [Theory]
    // A member who is not the owner can leave.
    [InlineData(true, false, true)]
    // The owner must transfer ownership instead.
    [InlineData(true, true, false)]
    // Non-members must not be offered Leave — the #125 regression.
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    public void CanLeave_RequiresMembershipAndNotOwner(bool isMember, bool isOwner, bool expected)
    {
        Assert.Equal(expected, TeamActionGate.CanLeave(isMember, isOwner));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void CanEditConsent_RequiresAdministrator(bool isAdministrator, bool expected)
    {
        Assert.Equal(expected, TeamActionGate.CanEditConsent(isAdministrator));
    }
}
