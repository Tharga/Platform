using System.Security.Claims;
using Tharga.Team;
using Tharga.Team.Service;

namespace Tharga.Team.Service.Tests.Audit;

/// <summary>
/// Invariants I4a–I4c: consent is necessary but not sufficient, proven at <b>every</b> level rather than
/// as consent/no-consent.
/// </summary>
/// <remarks>
/// <c>audit:read</c> is registered at <see cref="AccessLevel.Administrator"/>, so a team that consents at
/// <see cref="AccessLevel.User"/> has granted cross-team access and still granted no audit access. Testing
/// only "consent" against "no consent" would pass while every level below Administrator silently leaked
/// or silently refused, and nobody would know which.
/// <para>
/// <b>These cover C4 — a user with a system grant.</b> C6, the same question for a system API key, cannot
/// be expressed today: an API key principal carries no <c>ClaimTypes.Role</c> claims at all, and consent
/// matches roles, so no consent rule can ever fire for a key. That is recorded as a finding rather than
/// worked around here.
/// </para>
/// </remarks>
public class AuditConsentLevelTests
{
    private const string OtherTeam = "team-2";

    private sealed record FakeTeam(string Key, AccessLevel? ConsentAccessLevel) : ITeam
    {
        public string Name => Key;
        public string Icon => null;
        public string[] ConsentedRoles => ["Support"];
    }

    private static async IAsyncEnumerable<ITeam> Teams(params ITeam[] teams)
    {
        foreach (var t in teams) yield return t;
        await Task.CompletedTask;
    }

    private static ClaimsPrincipal WithRole(string role)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "Test"));

    /// <summary>
    /// Resolves what a consenting team grants a non-member holding the consented role, using the real
    /// scope registry so the level→scope mapping is the product's, not the test's.
    /// </summary>
    private static async Task<TeamGrant> ResolveAsync(AccessLevel? consentLevel, string callerRole = "Support")
    {
        var teamService = Substitute.For<ITeamService>();
        teamService.GetTeamMemberAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((ITeamMember)null);
        // Honours the roles it is given, because that is the contract: GetConsentedTeamsAsync returns the
        // teams that consented to one of the caller's roles, so role matching lives there and not in the
        // resolver. A fake that ignored them would make "a different role" untestable and would have
        // quietly asserted the resolver does matching it does not do.
        teamService.GetConsentedTeamsAsync(Arg.Any<string[]>())
            .Returns(ci =>
            {
                var roles = ci.Arg<string[]>() ?? [];
                if (consentLevel == null) return Teams();
                var team = new FakeTeam(OtherTeam, consentLevel);
                return roles.Intersect(team.ConsentedRoles).Any() ? Teams(team) : Teams();
            });

        var registry = new ScopeRegistry();
        registry.Register(AuditScopes.Read, AccessLevel.Administrator, "View the audit log.");

        var resolver = new TeamGrantResolver(teamService, registry);
        return await resolver.ResolveAsync(WithRole(callerRole), userKey: "u1", OtherTeam, AccessLevel.Viewer);
    }

    /// <summary>I4a — no consent, no access. The team simply is not reachable.</summary>
    [Fact]
    public async Task NoConsent_GrantsNothing()
    {
        Assert.Null(await ResolveAsync(consentLevel: null));
    }

    /// <summary>
    /// I4b — consent below the scope's level reaches the team and still carries no <c>audit:read</c>.
    /// This is the row that a consent/no-consent test would have got wrong.
    /// </summary>
    [Theory]
    [InlineData(AccessLevel.Viewer)]
    [InlineData(AccessLevel.User)]
    public async Task ConsentBelowAdministrator_ReachesTheTeamButNotItsAudit(AccessLevel level)
    {
        var grant = await ResolveAsync(level);

        Assert.NotNull(grant);
        Assert.Equal(level, grant.AccessLevel);
        Assert.DoesNotContain(AuditScopes.Read, grant.Scopes);
    }

    /// <summary>I4c — consent at the scope's level carries it, and the grant is at that level, not above.</summary>
    [Fact]
    public async Task ConsentAtAdministrator_CarriesAuditRead()
    {
        var grant = await ResolveAsync(AccessLevel.Administrator);

        Assert.NotNull(grant);
        Assert.Equal(AccessLevel.Administrator, grant.AccessLevel);
        Assert.Contains(AuditScopes.Read, grant.Scopes);
    }

    /// <summary>
    /// Consent is granted to named roles: holding a different one reaches nothing, whatever the level.
    /// </summary>
    [Fact]
    public async Task ADifferentRole_IsNotConsented()
    {
        Assert.Null(await ResolveAsync(AccessLevel.Administrator, callerRole: "Sales"));
    }

    /// <summary>
    /// A caller with no roles at all reaches nothing — <b>which is exactly the position every API key is
    /// in</b>, since the API-key authentication handler emits no role claims. Pinned here because it is
    /// the mechanism behind the C6 finding, not an incidental edge case.
    /// </summary>
    [Fact]
    public async Task ACallerWithNoRoles_ReachesNothing()
    {
        var teamService = Substitute.For<ITeamService>();
        teamService.GetTeamMemberAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((ITeamMember)null);
        teamService.GetConsentedTeamsAsync(Arg.Any<string[]>())
            .Returns(_ => Teams(new FakeTeam(OtherTeam, AccessLevel.Administrator)));

        var registry = new ScopeRegistry();
        registry.Register(AuditScopes.Read, AccessLevel.Administrator, "View the audit log.");

        var resolver = new TeamGrantResolver(teamService, registry);
        var grant = await resolver.ResolveAsync(
            new ClaimsPrincipal(new ClaimsIdentity([], "Test")), userKey: "u1", OtherTeam, AccessLevel.Viewer);

        Assert.Null(grant);
    }
}
