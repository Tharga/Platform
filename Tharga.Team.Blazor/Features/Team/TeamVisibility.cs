using System.Security.Claims;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Features.Team;

/// <summary>
/// How much access a team has consented to grant an oversight caller, reduced to the three states the
/// UI distinguishes.
/// </summary>
internal enum ConsentVisibility
{
    /// <summary>The team has consented to nothing — visible, but no access.</summary>
    None,

    /// <summary>Partial access (Viewer or User).</summary>
    Partial,

    /// <summary>Full team-administrator access.</summary>
    Full
}

/// <summary>
/// Decides what an oversight caller (one holding <see cref="SystemTeamScopes.Read"/>) may see: whether
/// team listings widen beyond their own memberships, and how a team's consent level is presented.
/// </summary>
/// <remarks>
/// Pure and static so it is unit-testable — this project has no bUnit, so any decision left inside
/// razor markup is unreachable from tests. Mirrors <c>TeamActionGate</c> and
/// <c>CreateTeamActionResolver</c>.
/// </remarks>
internal static class TeamVisibility
{
    /// <summary>
    /// Whether the caller may enumerate every team. Keyed on the scope, never on a role name — role
    /// names are host-configurable, so hard-coding one would break for any host that renames them.
    /// </summary>
    public static bool CanSeeAllTeams(ClaimsPrincipal principal)
    {
        return principal?.HasClaim(TeamClaimTypes.Scope, SystemTeamScopes.Read) ?? false;
    }

    /// <summary>
    /// Reduces a team's consented access level to the state shown in the UI. A team that has consented
    /// to no roles reads as <see cref="ConsentVisibility.None"/> regardless of any stored level.
    /// </summary>
    public static ConsentVisibility Resolve(string[] consentedRoles, AccessLevel? consentAccessLevel)
    {
        if (consentedRoles is not { Length: > 0 }) return ConsentVisibility.None;
        if (consentAccessLevel is null) return ConsentVisibility.Partial;

        return consentAccessLevel == AccessLevel.Administrator
            ? ConsentVisibility.Full
            : ConsentVisibility.Partial;
    }

    /// <summary>Label shown alongside the tint — colour alone is not an accessible encoding.</summary>
    public static string Label(ConsentVisibility visibility) => visibility switch
    {
        ConsentVisibility.Full => "Full access",
        ConsentVisibility.Partial => "Partial access",
        _ => "No access"
    };

    /// <summary>
    /// Radzen <c>BadgeStyle</c> name for the tint. Returned as a string so this stays free of a
    /// component-library dependency and remains unit-testable. Theme-aware by construction — hard-coded
    /// colours would not survive dark theme.
    /// </summary>
    public static string BadgeStyle(ConsentVisibility visibility) => visibility switch
    {
        ConsentVisibility.Full => "Success",
        ConsentVisibility.Partial => "Warning",
        _ => "Danger"
    };
}
