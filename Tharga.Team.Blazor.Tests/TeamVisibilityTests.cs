using System.Security.Claims;
using Tharga.Team;
using Tharga.Team.Blazor.Features.Team;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Tests for <see cref="TeamVisibility"/> — who may enumerate every team, and how a team's consent
/// level is reduced to the three states the UI shows.
/// </summary>
public class TeamVisibilityTests
{
    private static ClaimsPrincipal Principal(params string[] scopes)
    {
        var claims = scopes.Select(s => new Claim(TeamClaimTypes.Scope, s));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    [Fact]
    public void CanSeeAllTeams_WithTeamsRead_IsTrue()
    {
        Assert.True(TeamVisibility.CanSeeAllTeams(Principal(SystemTeamScopes.Read)));
    }

    [Fact]
    public void CanSeeAllTeams_WithoutTheScope_IsFalse()
    {
        Assert.False(TeamVisibility.CanSeeAllTeams(Principal(TeamScopes.Manage, SystemTeamScopes.Delete)));
    }

    [Fact]
    public void CanSeeAllTeams_NullPrincipal_IsFalse()
    {
        Assert.False(TeamVisibility.CanSeeAllTeams(null));
    }

    [Fact]
    public void CanSeeAllTeams_UnauthenticatedPrincipal_IsFalse()
    {
        Assert.False(TeamVisibility.CanSeeAllTeams(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    [Theory]
    // No consented roles -> nothing granted, whatever level is stored.
    [InlineData(false, null, "None")]
    [InlineData(false, AccessLevel.Administrator, "None")]
    // Consented, level absent -> falls back to the host default, which is never Administrator.
    [InlineData(true, null, "Partial")]
    // Consented at a partial level.
    [InlineData(true, AccessLevel.Viewer, "Partial")]
    [InlineData(true, AccessLevel.User, "Partial")]
    // Consented at full team-administrator access.
    [InlineData(true, AccessLevel.Administrator, "Full")]
    public void Resolve_ReducesConsentToTheThreeUiStates(bool hasRoles, AccessLevel? level, string expected)
    {
        var roles = hasRoles ? ["Developer"] : Array.Empty<string>();

        Assert.Equal(expected, TeamVisibility.Resolve(roles, level).ToString());
    }

    [Fact]
    public void Resolve_NullRoles_IsNone()
    {
        Assert.Equal("None", TeamVisibility.Resolve(null, AccessLevel.Administrator).ToString());
    }

    [Theory]
    [InlineData(false, null, "No access", "Danger")]
    [InlineData(true, AccessLevel.Viewer, "Partial access", "Warning")]
    [InlineData(true, AccessLevel.Administrator, "Full access", "Success")]
    public void LabelAndBadgeStyle_PairTextWithTint(bool hasRoles, AccessLevel? level, string label, string badgeStyle)
    {
        var roles = hasRoles ? ["Developer"] : Array.Empty<string>();
        var visibility = TeamVisibility.Resolve(roles, level);

        Assert.Equal(label, TeamVisibility.Label(visibility));
        Assert.Equal(badgeStyle, TeamVisibility.BadgeStyle(visibility));
    }
}
