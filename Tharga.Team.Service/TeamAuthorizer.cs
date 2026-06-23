using System.Security.Claims;
using Tharga.Team;

namespace Tharga.Team.Service;

/// <summary>
/// Service-layer authorization primitives for team operations, read from the caller's claims via
/// <see cref="ITeamPrincipalAccessor"/> (so they work for HTTP/API callers and interactive Blazor circuits
/// alike). The authorization decorator over <c>ITeamService</c> composes these per operation:
/// <list type="bullet">
/// <item><b>In-team scopes</b> (<see cref="TeamScopes.Manage"/>, <see cref="TeamScopes.MemberManage"/>, …)
/// authorize only the caller's <i>own</i> team — the <c>TeamKey</c> claim must equal the target
/// <c>teamKey</c>, closing the "admin of team A acts on team B" hole.</item>
/// <item><b>System scopes</b> (<see cref="SystemTeamScopes.Delete"/>) authorize across <i>any</i> team —
/// no team binding.</item>
/// </list>
/// Claims are the source of truth: scope claims are emitted from the caller's access level / roles /
/// overrides for their team (or from a system key's scope list), so a present scope claim already reflects
/// the underlying membership.
/// </summary>
public sealed class TeamAuthorizer
{
    private readonly ITeamPrincipalAccessor _principalAccessor;

    public TeamAuthorizer(ITeamPrincipalAccessor principalAccessor)
    {
        _principalAccessor = principalAccessor;
    }

    /// <summary>True when there is an authenticated caller (any identity).</summary>
    public async ValueTask<bool> IsAuthenticatedAsync()
    {
        var principal = await _principalAccessor.GetCurrentAsync();
        return principal?.Identity?.IsAuthenticated ?? false;
    }

    /// <summary>
    /// True when the caller holds <paramref name="scope"/> <b>for</b> <paramref name="teamKey"/>: the scope
    /// claim is present <b>and</b> the caller's <c>TeamKey</c> claim equals <paramref name="teamKey"/>. The
    /// scope only authorizes the caller's own team.
    /// </summary>
    public async ValueTask<bool> HasTeamScopeAsync(string scope, string teamKey)
    {
        var principal = await _principalAccessor.GetCurrentAsync();
        if (principal == null || string.IsNullOrEmpty(teamKey)) return false;

        var callerTeam = principal.FindFirst(TeamClaimTypes.TeamKey)?.Value;
        if (string.IsNullOrEmpty(callerTeam) || callerTeam != teamKey) return false;

        return HasScopeClaim(principal, scope);
    }

    /// <summary>True when the caller holds the system <paramref name="scope"/> (authorizes any team; no team binding).</summary>
    public async ValueTask<bool> HasSystemScopeAsync(string scope)
    {
        var principal = await _principalAccessor.GetCurrentAsync();
        return principal != null && HasScopeClaim(principal, scope);
    }

    private static bool HasScopeClaim(ClaimsPrincipal principal, string scope) =>
        principal.Claims.Any(c => c.Type == TeamClaimTypes.Scope && c.Value == scope);
}
