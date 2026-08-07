using System.Security.Claims;
using Tharga.Team;
using Tharga.Team.Blazor.Features.User;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Role classification for the profile page badges (Tharga/Team#155): app roles the caller holds durably,
/// separated from the ones synthesised out of whichever team is selected.
/// </summary>
/// <remarks>
/// The split is the substance of the feature. `TeamMember` and `Team{AccessLevel}` change as the caller
/// switches teams, so presenting them alongside app roles undifferentiated would read as a permanent grant.
/// </remarks>
public class ProfileRolesTests
{
    private static ClaimsPrincipal With(params string[] roles)
        => new(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "test"));

    [Fact]
    public void AppRoles_AndTeamRoles_AreSeparated()
    {
        var roles = ProfileRoles.Read(With("Developer", Roles.TeamMember, "TeamOwner", "Support"));

        Assert.Equal(["Developer", "Support"], roles.App);
        Assert.Equal([Roles.TeamMember, "TeamOwner"], roles.Team);
    }

    /// <summary>Every access level produces a team role, so a new level cannot quietly land in the app column.</summary>
    [Theory]
    [InlineData(AccessLevel.Owner)]
    [InlineData(AccessLevel.Administrator)]
    [InlineData(AccessLevel.User)]
    [InlineData(AccessLevel.Viewer)]
    [InlineData(AccessLevel.Custom)]
    public void EveryAccessLevelRole_IsTeamDerived(AccessLevel level)
    {
        var roles = ProfileRoles.Read(With("Team" + level));

        Assert.Empty(roles.App);
        Assert.Equal(["Team" + level], roles.Team);
    }

    /// <summary>
    /// A role that merely starts with "Team" is an app role. The old inline predicate tested the prefix before
    /// matching an access level, so this guards the intent of that ordering rather than its shape.
    /// </summary>
    [Theory]
    [InlineData("TeamLead")]
    [InlineData("Teamster")]
    [InlineData("Team")]
    public void ARoleThatOnlyLooksLikeATeamRole_StaysAnAppRole(string role)
    {
        var roles = ProfileRoles.Read(With(role));

        Assert.Equal([role], roles.App);
        Assert.Empty(roles.Team);
    }

    [Fact]
    public void Duplicates_AreCollapsed_AndOrderIsStable()
    {
        var roles = ProfileRoles.Read(With("Support", "Developer", "Support"));

        Assert.Equal(["Developer", "Support"], roles.App);
    }

    [Fact]
    public void NoRoles_ReportsNothingToRender()
    {
        var roles = ProfileRoles.Read(With());

        Assert.False(roles.Any);
        Assert.Empty(roles.App);
        Assert.Empty(roles.Team);
    }

    /// <summary>The page renders before the principal resolves, so this must not throw.</summary>
    [Fact]
    public void ANullPrincipal_IsEmpty()
    {
        Assert.False(ProfileRoles.Read(null).Any);
    }

    [Fact]
    public void AnEmptyRoleValue_IsIgnored()
    {
        var roles = ProfileRoles.Read(With("", "Developer"));

        Assert.Equal(["Developer"], roles.App);
    }

    /// <summary>Only role claims — the page has plenty of others, and none of them are roles.</summary>
    [Fact]
    public void NonRoleClaims_AreIgnored()
    {
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.Role, "Developer"),
            new Claim(ClaimTypes.Email, "a@test.com"),
            new Claim(TeamClaimTypes.Scope, "team:read")
        ], "test");

        var roles = ProfileRoles.Read(new ClaimsPrincipal(identity));

        Assert.Equal(["Developer"], roles.App);
        Assert.Empty(roles.Team);
    }
}
