using System.Security.Claims;
using Tharga.Team;

namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// Whether a component should offer an action, given the caller's claims. Mirrors what the service layer
/// enforces, so a surface does not render controls the server will reject.
/// </summary>
/// <remarks>
/// The distinction this exists to make explicit: an in-team scope is issued for the selected team only,
/// so a bare <c>HasClaim(Scope, x)</c> answers "holds it somewhere", not "holds it here". Gating on the
/// bare claim is how the API key view came to render for a caller with no access to the selected team —
/// its <c>apikey:manage</c> came from a system role, which is team-independent by design. Use
/// <see cref="HasTeamScope"/> for anything acting on a team, and <see cref="HasSystemScope"/> only where
/// the operation genuinely spans the system.
/// </remarks>
public static class TeamScopeGate
{
    /// <summary>
    /// Whether the caller holds <paramref name="scope"/> <b>for</b> <paramref name="teamKey"/> — the claim
    /// is present and it was issued for that team.
    /// </summary>
    public static bool HasTeamScope(ClaimsPrincipal principal, string scope, string teamKey)
    {
        if (principal == null || string.IsNullOrEmpty(teamKey)) return false;

        var callerTeam = principal.FindFirst(TeamClaimTypes.TeamKey)?.Value;
        if (string.IsNullOrEmpty(callerTeam) || callerTeam != teamKey) return false;

        return principal.HasClaim(TeamClaimTypes.Scope, scope);
    }

    /// <summary>
    /// Whether the caller holds the system <paramref name="scope"/>. Not bound to any team — use only for
    /// operations that genuinely act across the system.
    /// </summary>
    public static bool HasSystemScope(ClaimsPrincipal principal, string scope)
        => principal?.HasClaim(TeamClaimTypes.Scope, scope) ?? false;
}
