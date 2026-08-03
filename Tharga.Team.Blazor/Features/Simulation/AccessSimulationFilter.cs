using System.Security.Claims;
using Tharga.Team;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Features.Simulation;

/// <summary>
/// Narrows an already-issued principal to the access being simulated.
/// </summary>
/// <remarks>
/// <b>Scopes are strictly subtractive. There is no code path here that adds a scope claim</b> — team or
/// system. That is what makes de-escalation a property of this type rather than of a calculation being
/// right: a bug can only remove too much, which the caller sees immediately and reverses. It is also why
/// the simulation may travel in a cookie the caller can edit — forging one can only take more away.
/// <para>
/// <b>The access level is the one exception, and it is a clamped replacement rather than a removal.</b>
/// It is a single value that <c>[RequireAccessLevel]</c> reads directly, and the matching
/// <c>Team{Level}</c> role is what <c>AuthorizeView Roles="…"</c> matches, so a simulation that only
/// removed them would show a caller with no level at all rather than a lower one. Both move together,
/// through <see cref="AccessLevelPrivilege.Clamp"/>, so they cannot disagree and neither can rise.
/// </para>
/// <para>
/// Identity claims — name, subject, email, <see cref="TeamClaimTypes.MemberKey"/>,
/// <see cref="TeamClaimTypes.TeamKey"/> — are never touched. The audit actor therefore stays the real
/// caller by construction, not by a rule someone has to remember to apply.
/// </para>
/// <para>
/// <b>Every identity on the principal is filtered, not just the primary one.</b> Authorization reads
/// <c>ClaimsPrincipal.Claims</c>, which is the union across identities, so a scope left on a secondary
/// identity would still be honoured — the simulation would appear to work while granting the scope it
/// claimed to remove.
/// </para>
/// </remarks>
internal static class AccessSimulationFilter
{
    /// <summary>
    /// Applies <paramref name="simulation"/> to <paramref name="principal"/> in place. A null simulation
    /// leaves it untouched.
    /// </summary>
    public static void Apply(ClaimsPrincipal principal, AccessSimulation simulation)
    {
        if (principal == null || simulation == null) return;

        foreach (var identity in principal.Identities)
        {
            ApplyTo(identity, simulation);
        }
    }

    private static void ApplyTo(ClaimsIdentity identity, AccessSimulation simulation)
    {
        var kept = new HashSet<string>(simulation.Scopes ?? [], StringComparer.OrdinalIgnoreCase);

        RemoveWhere(identity, c => c.Type == TeamClaimTypes.Scope && !kept.Contains(c.Value));

        if (simulation.DropSystemScopes)
            RemoveWhere(identity, c => c.Type == TeamClaimTypes.SystemScope);

        if (simulation.DropAppRoles)
            RemoveWhere(identity, c => c.Type == ClaimTypes.Role && !IsTeamRole(c.Value));

        ApplyAccessLevel(identity, simulation.AccessLevel);
    }

    /// <remarks>
    /// The only place a claim is added, and it can only ever replace a level with a less privileged one.
    /// If the requested level is an escalation, <see cref="AccessLevelPrivilege.Clamp"/> returns the real
    /// level and nothing changes.
    /// </remarks>
    private static void ApplyAccessLevel(ClaimsIdentity identity, AccessLevel? requested)
    {
        if (requested == null) return;

        var current = identity.FindFirst(TeamClaimTypes.AccessLevel)?.Value;
        if (!Enum.TryParse<AccessLevel>(current, out var actual)) return;

        var effective = AccessLevelPrivilege.Clamp(requested.Value, actual);
        if (effective == actual) return;

        RemoveWhere(identity, c => c.Type == TeamClaimTypes.AccessLevel);
        RemoveWhere(identity, c => c.Type == ClaimTypes.Role && IsAccessLevelRole(c.Value));

        identity.AddClaim(new Claim(TeamClaimTypes.AccessLevel, effective.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Role, $"Team{effective}"));
    }

    /// <summary>
    /// Roles the toolkit derives from team membership, as opposed to the app roles an identity provider
    /// issued. Team roles follow the simulated access level; app roles are the caller's own.
    /// </summary>
    // Qualified: there is a Features.Roles namespace, so a bare `Roles` binds to it rather than to the
    // Framework class.
    private static bool IsTeamRole(string role)
        => role == Framework.Roles.TeamMember || IsAccessLevelRole(role);

    private static bool IsAccessLevelRole(string role)
        => Enum.GetNames<AccessLevel>().Any(level => role == $"Team{level}");

    /// <remarks>
    /// <see cref="ClaimsIdentity.TryRemoveClaim"/> refuses a claim it does not own. Every claim reached
    /// here comes from <c>identity.Claims</c>, so it is owned — but the return value is checked anyway,
    /// because a silently surviving scope claim is the one failure this type exists to prevent.
    /// </remarks>
    private static void RemoveWhere(ClaimsIdentity identity, Func<Claim, bool> predicate)
    {
        foreach (var claim in identity.Claims.Where(predicate).ToArray())
        {
            if (!identity.TryRemoveClaim(claim))
            {
                throw new InvalidOperationException(
                    $"Access simulation could not remove the claim '{claim.Type}'. Refusing to continue: " +
                    "a simulation that silently keeps a claim it reported removing would show more access than it states.");
            }
        }
    }
}
