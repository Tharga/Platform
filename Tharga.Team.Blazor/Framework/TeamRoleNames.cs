using Tharga.Team;

namespace Tharga.Team.Blazor.Framework;

/// <summary>
/// Which role values the toolkit synthesises from the selected team, as opposed to app roles the caller
/// holds durably.
/// </summary>
/// <remarks>
/// <b>Extracted so there is one copy of the rule.</b> <c>TeamClaimRevalidator</c> needs it to decide which
/// claims to replace on a refresh, and the profile page needs it to avoid presenting a role that changes with
/// the team selector as though it were a permanent grant. Two implementations of "is this a team role" would
/// drift, and a role misclassified in one place but not the other is the kind of difference nobody notices
/// until it matters.
/// </remarks>
internal static class TeamRoleNames
{
    private static readonly string[] AccessLevelRoles =
        [.. Enum.GetNames<AccessLevel>().Select(name => "Team" + name)];

    /// <summary>
    /// Whether <paramref name="roleValue"/> is synthesised from the caller's membership of the selected team —
    /// <see cref="Roles.TeamMember"/> or <c>Team{AccessLevel}</c> — rather than held as an app role.
    /// </summary>
    public static bool IsTeamDerived(string roleValue)
    {
        if (string.IsNullOrEmpty(roleValue)) return false;

        return roleValue == Roles.TeamMember || AccessLevelRoles.Contains(roleValue, StringComparer.Ordinal);
    }
}
