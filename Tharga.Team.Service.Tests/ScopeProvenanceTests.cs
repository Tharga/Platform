using System.Security.Claims;
using Tharga.Team;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// A scope granted by an app role and the same scope granted by a team access level are different
/// authorizations, and must not satisfy each other's checks.
/// </summary>
/// <remarks>
/// Both were once emitted as <c>TeamClaimTypes.Scope</c>, so nothing downstream could tell them apart. The
/// consequences were live: <c>audit:read</c> is registered at Administrator level *and* mapped to a system
/// role, so a team administrator satisfied the gate on an unpinned audit view and could read every team's
/// entries. <c>apikey:manage</c> had the same shape — a Developer at <c>AccessLevel.User</c> managed a team's
/// API keys, bypassing the Administrator requirement the team registry declares.
/// </remarks>
public class ScopeProvenanceTests
{
    private const string Scope = "audit:read";
    private const string TeamKey = "team-a";

    private static ClaimsPrincipal TeamGrant(string scope, string teamKey = TeamKey)
        => new(new ClaimsIdentity(
            [new Claim(TeamClaimTypes.TeamKey, teamKey), new Claim(TeamClaimTypes.Scope, scope)], "Test"));

    private static ClaimsPrincipal SystemGrant(string scope, string teamKey = TeamKey)
    {
        var claims = new List<Claim> { new(TeamClaimTypes.SystemScope, scope) };
        if (teamKey != null) claims.Add(new Claim(TeamClaimTypes.TeamKey, teamKey));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    // ---- The defect ----

    [Fact]
    public void TeamGrant_DoesNotSatisfy_SystemCheck()
    {
        Assert.False(TeamScopePolicy.HasSystemScope(TeamGrant(Scope), Scope));
    }

    [Fact]
    public void SystemGrant_DoesNotSatisfy_TeamCheck()
    {
        // A system role is not membership. It must not stand in for a scope issued on a specific team.
        Assert.False(TeamScopePolicy.HasTeamScope(SystemGrant(Scope), Scope, TeamKey));
    }

    // ---- Each still satisfies its own check ----

    [Fact]
    public void TeamGrant_SatisfiesTeamCheck_ForThatTeam()
    {
        Assert.True(TeamScopePolicy.HasTeamScope(TeamGrant(Scope), Scope, TeamKey));
    }

    [Fact]
    public void TeamGrant_DoesNotSatisfyTeamCheck_ForAnotherTeam()
    {
        Assert.False(TeamScopePolicy.HasTeamScope(TeamGrant(Scope), Scope, "team-b"));
    }

    [Fact]
    public void SystemGrant_SatisfiesSystemCheck()
    {
        Assert.True(TeamScopePolicy.HasSystemScope(SystemGrant(Scope), Scope));
    }

    [Fact]
    public void SystemGrant_SatisfiesSystemCheck_WithNoTeamSelected()
    {
        Assert.True(TeamScopePolicy.HasSystemScope(SystemGrant(Scope, teamKey: null), Scope));
    }

    // ---- Holding both is legitimate and keeps both ----

    [Fact]
    public void HoldingBoth_SatisfiesBoth()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(TeamClaimTypes.TeamKey, TeamKey),
            new Claim(TeamClaimTypes.Scope, Scope),
            new Claim(TeamClaimTypes.SystemScope, Scope)
        ], "Test"));

        Assert.True(TeamScopePolicy.HasTeamScope(principal, Scope, TeamKey));
        Assert.True(TeamScopePolicy.HasSystemScope(principal, Scope));
    }
}
