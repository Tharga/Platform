using System.Security.Claims;
using Microsoft.Extensions.Options;
using Tharga.Team;
using Tharga.Team.Service;
using Tharga.Team.Service.Audit;

namespace Tharga.Team.Service.Tests.Audit;

/// <summary>
/// One set of expectations, applied to the enforcement point every surface now shares.
/// </summary>
/// <remarks>
/// Invariant <b>I5</b> — <i>UI, MCP and REST give the same answer for the same caller</i> — used to need
/// proving three times, because each surface decided for itself: the UI and REST asked
/// <c>AuditAccess.CanRead</c>, and MCP asked whether the caller held a host-configurable role. The same
/// API key got different answers from different doors.
/// <para>
/// <b>Now there is one place to test.</b> All three inject <c>IAuditReadService</c> or
/// <c>IAuditOversightService</c> and gate nothing themselves, so agreement is a property of the shape.
/// These tests cover that shared point; <see cref="AuditSurfacesDelegateTests"/> covers the claim that
/// no surface has kept a rule of its own.
/// </para>
/// </remarks>
public class AuditGateSharedTests
{
    private const string OwnTeam = "team-1";
    private const string OtherTeam = "team-2";

    private static ClaimsPrincipal Caller(params Claim[] claims)
        => new(new ClaimsIdentity(claims, "Test"));

    private static Claim TeamKey(string key) => new(TeamClaimTypes.TeamKey, key);
    private static Claim TeamScope(string scope) => new(TeamClaimTypes.Scope, scope);
    private static Claim SystemScope(string scope) => new(TeamClaimTypes.SystemScope, scope);

    /// <summary>
    /// A substituted composite. The real one only queries a <c>MongoDbAuditLogger</c>, so a plain
    /// <c>IAuditLogger</c> substitute would be silently ignored and every query assertion would pass
    /// against nothing.
    /// </summary>
    private static CompositeAuditLogger Composite()
    {
        var composite = Substitute.For<CompositeAuditLogger>(
            (IEnumerable<IAuditLogger>)[], Options.Create(new AuditOptions()), null, null);
        composite.QueryAsync(Arg.Any<AuditQuery>()).Returns(new AuditQueryResult());
        return composite;
    }

    private static (IAuditReadService Team, IAuditOversightService System) Build(ClaimsPrincipal principal)
    {
        var accessor = Substitute.For<ITeamPrincipalAccessor>();
        accessor.GetCurrentAsync().Returns(new ValueTask<ClaimsPrincipal>(principal));

        var composite = Composite();
        var service = new AuditReadService(composite);

        return (
            ScopeProxy<IAuditReadService>.Create(service, accessor, ServiceScopeKind.Team),
            ScopeProxy<IAuditOversightService>.Create(service, accessor, ServiceScopeKind.System));
    }

    // ---------------- C1 / C2: no grant at all ----------------

    [Fact]
    public async Task Anonymous_ReadsNothing()
    {
        var (team, system) = Build(Caller());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => team.QueryAsync(OwnTeam, new AuditQuery()));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => system.QueryAllAsync(new AuditQuery()));
    }

    [Fact]
    public async Task AMemberWithoutTheGrant_ReadsNothing()
    {
        var (team, system) = Build(Caller(TeamKey(OwnTeam)));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => team.QueryAsync(OwnTeam, new AuditQuery()));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => system.QueryAllAsync(new AuditQuery()));
    }

    // ---------------- C3 / C5: a team grant ----------------

    [Fact]
    public async Task ATeamGrant_ReadsItsOwnTeam()
    {
        var (team, _) = Build(Caller(TeamKey(OwnTeam), TeamScope(AuditScopes.Read)));

        Assert.NotNull(await team.QueryAsync(OwnTeam, new AuditQuery()));
    }

    /// <summary>Invariant <b>I3</b>: a team grant reaches only the team it was issued for.</summary>
    [Fact]
    public async Task ATeamGrant_DoesNotReachAnotherTeam()
    {
        var (team, _) = Build(Caller(TeamKey(OwnTeam), TeamScope(AuditScopes.Read)));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => team.QueryAsync(OtherTeam, new AuditQuery()));
    }

    /// <summary>
    /// Invariant <b>I1</b>: a team grant never reaches system-wide audit — and it cannot even ask, because
    /// the oversight interface takes no team and is registered as a system service.
    /// </summary>
    [Fact]
    public async Task ATeamGrant_NeverReachesSystemWideAudit()
    {
        var (_, system) = Build(Caller(TeamKey(OwnTeam), TeamScope(AuditScopes.Read)));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => system.QueryAllAsync(new AuditQuery()));
    }

    /// <summary>
    /// A <i>team</i> grant literally named <c>audit:read</c> does not satisfy the system check. The claim
    /// types carry provenance precisely so an in-team scope cannot be spent across teams.
    /// </summary>
    [Fact]
    public async Task ATeamGrantOfTheSameName_DoesNotSatisfyTheSystemCheck()
    {
        var (_, system) = Build(Caller(TeamKey(OwnTeam), TeamScope(AuditScopes.Read)));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => system.QueryAllAsync(new AuditQuery()));
    }

    // ---------------- C4 / C6: a system grant ----------------

    [Fact]
    public async Task ASystemGrant_ReadsEveryTeam()
    {
        var (_, system) = Build(Caller(SystemScope(AuditScopes.Read)));

        Assert.NotNull(await system.QueryAllAsync(new AuditQuery()));
    }

    /// <summary>
    /// A system grant reads one named team by <i>filtering</i> the oversight read, not through the
    /// team-bound service.
    /// </summary>
    /// <remarks>
    /// <c>ScopeProxy</c>'s team check does not accept a system grant, and that must not change: the
    /// provenance split exists so an in-team scope cannot be spent cross-team, and loosening it globally
    /// would undo it in every service at once. So the union that <c>AuditAccess</c> used to express in
    /// one method is now two interfaces, and the caller reaches one team through the one it qualifies
    /// for.
    /// </remarks>
    [Fact]
    public async Task ASystemGrant_ReadsOneNamedTeam_ByFilteringTheOversightRead()
    {
        var (team, system) = Build(Caller(SystemScope(AuditScopes.Read)));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => team.QueryAsync(OtherTeam, new AuditQuery()));

        Assert.NotNull(await system.QueryAllAsync(new AuditQuery { TeamKey = OtherTeam }));
    }

    // ---------------- the query cannot widen past the check ----------------

    /// <summary>
    /// The team is taken from the argument the caller was authorized against, never from the query. A
    /// query naming another team would otherwise read a team the scope check never saw — authorization
    /// against one value and a read against another.
    /// </summary>
    [Fact]
    public async Task AQueryNamingAnotherTeam_IsOverwrittenWithTheAuthorizedOne()
    {
        var composite = Composite();
        var service = new AuditReadService(composite);

        await service.QueryAsync(OwnTeam, new AuditQuery { TeamKey = OtherTeam });

        await composite.Received(1).QueryAsync(Arg.Is<AuditQuery>(q => q.TeamKey == OwnTeam));
    }

    /// <summary>
    /// The oversight read honours a team key as a <i>filter</i>.
    /// </summary>
    /// <remarks>
    /// This test asserted the opposite first, and the opposite was wrong. Clearing the team looked
    /// tidier — "one read, one authorization" — but it left a system-grant holder unable to ask about a
    /// single team at all: the team-bound service refuses them (<c>ScopeProxy</c> does not accept a
    /// system grant for a team check) and the oversight one ignored their filter. They would have had to
    /// fetch every team and narrow it client-side, which <c>AuditAccess</c> never made them do.
    /// </remarks>
    [Fact]
    public async Task TheOversightQuery_HonoursATeamKeyAsAFilter()
    {
        var composite = Composite();
        var service = new AuditReadService(composite);

        await service.QueryAllAsync(new AuditQuery { TeamKey = OwnTeam });

        await composite.Received(1).QueryAsync(Arg.Is<AuditQuery>(q => q.TeamKey == OwnTeam));
    }
}
