using System.Security.Claims;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Service.Tests.Audit;

/// <summary>
/// Who may read the audit log, and for which team. Shared by the Blazor view and the REST endpoint, so
/// these assertions cover both surfaces at once — which is the point of having one rule.
/// </summary>
public class AuditAccessTests
{
    private const string TeamA = "team-a";
    private const string TeamB = "team-b";

    [Fact]
    public void NoPrincipal_CannotRead()
    {
        Assert.False(AuditAccess.CanRead(null, TeamA));
        Assert.False(AuditAccess.CanRead(null, null));
    }

    [Fact]
    public void AnonymousPrincipal_CannotRead()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.False(AuditAccess.CanRead(anonymous, TeamA));
        Assert.False(AuditAccess.CanRead(anonymous, null));
    }

    [Fact]
    public void AuthenticatedWithoutTheScope_CannotRead()
    {
        var caller = Principal(teamKey: TeamA);

        Assert.False(AuditAccess.CanRead(caller, TeamA));
        Assert.False(AuditAccess.CanRead(caller, null));
    }

    [Fact]
    public void ATeamGrant_ReadsItsOwnTeam()
    {
        var caller = Principal(teamKey: TeamA, teamScopes: [AuditScopes.Read]);

        Assert.True(AuditAccess.CanRead(caller, TeamA));
    }

    /// <summary>
    /// <b>I3.</b> A team grant is issued for the selected team, so it must not reach another one — even
    /// when the caller names that team explicitly. Knowing a team key is not authority over it.
    /// </summary>
    [Fact]
    public void ATeamGrant_CannotReachAnotherTeam()
    {
        var caller = Principal(teamKey: TeamA, teamScopes: [AuditScopes.Read]);

        Assert.False(AuditAccess.CanRead(caller, TeamB));
    }

    /// <summary>
    /// <b>I1.</b> Querying without a team spans every team, so a team grant must not satisfy it. Accepting
    /// one here is precisely the hole the Scope / SystemScope provenance split closed: a team
    /// administrator could read the whole system's log.
    /// </summary>
    [Fact]
    public void ATeamGrant_CannotReadAcrossAllTeams()
    {
        var caller = Principal(teamKey: TeamA, teamScopes: [AuditScopes.Read]);

        Assert.False(AuditAccess.CanRead(caller, teamKey: null));
    }

    [Fact]
    public void ASystemGrant_ReadsAcrossAllTeams()
    {
        var caller = Principal(systemScopes: [AuditScopes.Read]);

        Assert.True(AuditAccess.CanRead(caller, teamKey: null));
    }

    /// <summary>A system grant is team-independent, so it reaches any named team as well.</summary>
    [Theory]
    [InlineData(TeamA)]
    [InlineData(TeamB)]
    public void ASystemGrant_ReadsAnyNamedTeam(string teamKey)
    {
        var caller = Principal(systemScopes: [AuditScopes.Read]);

        Assert.True(AuditAccess.CanRead(caller, teamKey));
    }

    /// <summary>
    /// The scope must be the right one. Holding some other audit-adjacent grant is not holding this one.
    /// </summary>
    [Fact]
    public void ADifferentScope_DoesNotGrantAuditRead()
    {
        var caller = Principal(teamKey: TeamA, teamScopes: ["team:manage"], systemScopes: ["users:manage"]);

        Assert.False(AuditAccess.CanRead(caller, TeamA));
        Assert.False(AuditAccess.CanRead(caller, null));
    }

    private static ClaimsPrincipal Principal(string teamKey = null, string[] teamScopes = null, string[] systemScopes = null)
    {
        var claims = new List<Claim>();
        if (teamKey != null) claims.Add(new Claim(TeamClaimTypes.TeamKey, teamKey));
        foreach (var scope in teamScopes ?? []) claims.Add(new Claim(TeamClaimTypes.Scope, scope));
        foreach (var scope in systemScopes ?? []) claims.Add(new Claim(TeamClaimTypes.SystemScope, scope));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }
}
