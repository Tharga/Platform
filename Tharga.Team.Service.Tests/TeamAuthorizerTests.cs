using System.Security.Claims;
using Tharga.Team;
using Tharga.Team.Service;

namespace Tharga.Team.Service.Tests;

/// <summary>
/// Tests the service-layer authorization primitives: in-team scopes are bound to the caller's own team
/// (TeamKey must match), and system scopes authorize across any team.
/// </summary>
public class TeamAuthorizerTests
{
    private static TeamAuthorizer ForPrincipal(ClaimsPrincipal principal)
    {
        var accessor = Substitute.For<ITeamPrincipalAccessor>();
        accessor.GetCurrentAsync().Returns(new ValueTask<ClaimsPrincipal>(principal));
        return new TeamAuthorizer(accessor);
    }

    private static ClaimsPrincipal Principal(string teamKey, params string[] scopes)
    {
        var claims = new List<Claim>();
        if (teamKey != null) claims.Add(new Claim(TeamClaimTypes.TeamKey, teamKey));
        foreach (var s in scopes) claims.Add(new Claim(TeamClaimTypes.Scope, s));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    [Fact]
    public async Task HasTeamScope_True_WhenScopeAndTeamKeyMatch()
        => Assert.True(await ForPrincipal(Principal("T1", TeamScopes.Manage)).HasTeamScopeAsync(TeamScopes.Manage, "T1"));

    [Fact]
    public async Task HasTeamScope_False_ForDifferentTeam() // admin of T1 cannot act on T2
        => Assert.False(await ForPrincipal(Principal("T1", TeamScopes.Manage)).HasTeamScopeAsync(TeamScopes.Manage, "T2"));

    [Fact]
    public async Task HasTeamScope_False_WithoutTheScope()
        => Assert.False(await ForPrincipal(Principal("T1", TeamScopes.Read)).HasTeamScopeAsync(TeamScopes.Manage, "T1"));

    [Fact]
    public async Task HasTeamScope_False_WithoutTeamKey()
        => Assert.False(await ForPrincipal(Principal(null, TeamScopes.Manage)).HasTeamScopeAsync(TeamScopes.Manage, "T1"));

    [Fact]
    public async Task HasSystemScope_True_RegardlessOfTeam()
        => Assert.True(await ForPrincipal(Principal(null, SystemTeamScopes.Delete)).HasSystemScopeAsync(SystemTeamScopes.Delete));

    [Fact]
    public async Task HasSystemScope_False_WhenAbsent()
        => Assert.False(await ForPrincipal(Principal("T1", TeamScopes.Manage)).HasSystemScopeAsync(SystemTeamScopes.Delete));

    [Fact]
    public async Task IsAuthenticated_True_ForAuthenticatedIdentity()
        => Assert.True(await ForPrincipal(Principal("T1", TeamScopes.Manage)).IsAuthenticatedAsync());

    [Fact]
    public async Task IsAuthenticated_False_ForAnonymous()
        => Assert.False(await ForPrincipal(new ClaimsPrincipal(new ClaimsIdentity())).IsAuthenticatedAsync());
}
