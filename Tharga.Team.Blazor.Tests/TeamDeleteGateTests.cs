using System.Security.Claims;
using Tharga.Team;
using Tharga.Team.Blazor.Features.User;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Tests;

/// <summary>
/// Deleting a team from the Teams tab is a system-operator capability, not something a team consents to.
/// These pin the two halves of that: the scope must be <see cref="SystemTeamScopes.Delete"/>, and it must
/// be held as a <i>system</i> grant — an in-team claim of the same name is a different privilege and must
/// not open the action.
/// </summary>
public class TeamDeleteGateTests
{
    private static ClaimsPrincipal SystemPrincipal(params string[] systemScopes)
    {
        var claims = systemScopes.Select(s => new Claim(TeamClaimTypes.SystemScope, s));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static ClaimsPrincipal InTeamPrincipal(string teamKey, params string[] scopes)
    {
        var claims = new List<Claim> { new(TeamClaimTypes.TeamKey, teamKey) };
        claims.AddRange(scopes.Select(s => new Claim(TeamClaimTypes.Scope, s)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static bool Gate(ClaimsPrincipal principal)
        => UserAdminGate.CanDeleteTeams(TeamScopeGate.HasSystemScope(principal, SystemTeamScopes.Delete));

    [Fact]
    public void WithTheSystemDeleteScope_IsOffered()
    {
        Assert.True(Gate(SystemPrincipal(SystemTeamScopes.Delete)));
    }

    [Fact]
    public void WithoutTheScope_IsNotOffered()
    {
        Assert.False(Gate(SystemPrincipal()));
    }

    [Fact]
    public void Anonymous_IsNotOffered()
    {
        Assert.False(Gate(null));
    }

    /// <summary>
    /// The provenance rule. A team may grant <c>teams:delete</c> at an access level; that authorizes
    /// nothing across teams, so it must not open a cross-team delete action.
    /// </summary>
    [Fact]
    public void HoldingTheScopeInTeamOnly_IsNotOffered()
    {
        Assert.False(Gate(InTeamPrincipal("team-a", SystemTeamScopes.Delete)));
    }

    /// <summary>
    /// Seeing the tab and being able to destroy what it lists are separate privileges: viewing requires
    /// <c>users:manage</c>, deleting requires <c>teams:delete</c>.
    /// </summary>
    [Fact]
    public void UsersManageAlone_IsNotOffered()
    {
        Assert.False(Gate(SystemPrincipal(SystemUserScopes.Manage)));
    }

    /// <summary>
    /// Enumerating every team is discovery; it carries no power to delete one.
    /// </summary>
    [Fact]
    public void TeamsReadAlone_IsNotOffered()
    {
        Assert.False(Gate(SystemPrincipal(SystemTeamScopes.Read)));
    }

    [Fact]
    public void AlongsideOtherSystemScopes_IsOffered()
    {
        Assert.True(Gate(SystemPrincipal(SystemUserScopes.Manage, SystemTeamScopes.Read, SystemTeamScopes.Delete)));
    }
}
