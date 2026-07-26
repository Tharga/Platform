using System.Security.Claims;

namespace Tharga.Team.Service;

/// <summary>
/// The single decision for "may this principal use this scope" — pure, principal-in, so every enforcement
/// path shares one implementation rather than restating the policy.
/// </summary>
/// <remarks>
/// Two callers with different shapes need the same answer: <see cref="TeamAuthorizer"/> resolves the
/// principal asynchronously (a Blazor circuit cannot be read synchronously) and delegates here, while the
/// enforcement proxies already hold a resolved principal and call it directly. Restating the policy in
/// both is how they came to disagree — the proxy checked that *a* team was selected and the scope claim
/// existed somewhere, which authorized acting on any team.
/// </remarks>
internal static class TeamScopePolicy
{
    /// <summary>
    /// Whether the caller holds <paramref name="scope"/> <b>for</b> <paramref name="teamKey"/>: the scope
    /// claim is present and the caller's <c>TeamKey</c> claim is that same team. In-team scopes are issued
    /// for the selected team only, so this is what confines them to it.
    /// </summary>
    public static bool HasTeamScope(ClaimsPrincipal principal, string scope, string teamKey)
    {
        if (principal == null || string.IsNullOrEmpty(teamKey)) return false;

        var callerTeam = principal.FindFirst(TeamClaimTypes.TeamKey)?.Value;
        if (string.IsNullOrEmpty(callerTeam) || callerTeam != teamKey) return false;

        return HasScopeClaim(principal, scope);
    }

    /// <summary>
    /// Whether the caller holds the system <paramref name="scope"/>. Authorizes across any team and
    /// requires no team to be selected — system scopes come from app roles, independently of membership.
    /// </summary>
    public static bool HasSystemScope(ClaimsPrincipal principal, string scope)
        => principal != null && HasScopeClaim(principal, scope);

    private static bool HasScopeClaim(ClaimsPrincipal principal, string scope)
        => principal.Claims.Any(c => c.Type == TeamClaimTypes.Scope && c.Value == scope);
}
