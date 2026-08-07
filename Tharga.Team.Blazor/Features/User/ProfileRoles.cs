using System.Security.Claims;
using Tharga.Team.Blazor.Framework;

namespace Tharga.Team.Blazor.Features.User;

/// <summary>
/// The caller's roles, split into the ones they hold durably and the ones synthesised from the team they
/// currently have selected.
/// </summary>
/// <param name="App">Roles the caller holds regardless of team, sorted.</param>
/// <param name="Team">Roles derived from the selected team, sorted. These change when the selection changes.</param>
internal readonly record struct ProfileRoleSet(string[] App, string[] Team)
{
    public bool Any => App.Length > 0 || Team.Length > 0;
}

/// <summary>
/// Reads role claims for display on the profile page.
/// </summary>
/// <remarks>
/// <b>Split rather than one flat row</b> (Tharga/Team#155 left this open to the implementer).
/// <c>TeamMembershipClaimsBuilder</c> synthesises <c>TeamMember</c> and <c>Team{AccessLevel}</c> from whichever
/// team is selected, so those values change as the caller switches teams while app roles do not. Listing them
/// undifferentiated misrepresents both — a team role reads as a permanent grant, and an app role reads as
/// something the team selector might take away.
/// <para>
/// Pure and separately testable, so the classification is asserted rather than living in markup where nothing
/// can reach it.
/// </para>
/// </remarks>
internal static class ProfileRoles
{
    public static ProfileRoleSet Read(ClaimsPrincipal principal)
    {
        if (principal == null) return new ProfileRoleSet([], []);

        var roles = principal.FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        return new ProfileRoleSet(
            App: [.. roles.Where(x => !TeamRoleNames.IsTeamDerived(x))],
            Team: [.. roles.Where(TeamRoleNames.IsTeamDerived)]);
    }
}
