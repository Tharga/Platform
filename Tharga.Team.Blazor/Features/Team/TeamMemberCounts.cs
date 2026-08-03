namespace Tharga.Team.Blazor.Features.Team;

/// <summary>
/// How many members a team has, and how many of those are still invitations.
/// </summary>
/// <param name="Members">Accepted members.</param>
/// <param name="Invited">Outstanding invitations.</param>
/// <param name="Suspended">Members whose access is suspended.</param>
public readonly record struct TeamMemberCount(int Members, int Invited, int Suspended)
{
    /// <summary>What the column shows: the member count, with a pending count when there is one.</summary>
    /// <remarks>
    /// A single number would count invitations as members, which overstates a team that has just been
    /// set up — the most common moment for someone to be looking at this list.
    /// </remarks>
    public string Text => Invited > 0 ? $"{Members} (+{Invited})" : Members.ToString();

    /// <summary>The tooltip, spelling out what the parenthetical means.</summary>
    public string Title
    {
        get
        {
            var parts = new List<string> { $"{Members} member{(Members == 1 ? "" : "s")}" };
            if (Invited > 0) parts.Add($"{Invited} invited");
            if (Suspended > 0) parts.Add($"{Suspended} suspended");
            return string.Join(", ", parts);
        }
    }
}

/// <summary>
/// Counts a team's roster for display.
/// </summary>
/// <remarks>
/// Pure and static so it is testable — the project has no bUnit, so a rule left in razor markup is
/// unreachable from tests. Mirrors <see cref="TeamVisibility"/>, <see cref="TeamSelectorGate"/> and
/// <see cref="TeamListPresentation"/>.
/// </remarks>
internal static class TeamMemberCounts
{
    /// <summary>Counts a roster, tolerating a null one.</summary>
    public static TeamMemberCount Of(IEnumerable<ITeamMember> members)
    {
        if (members == null) return new TeamMemberCount(0, 0, 0);

        var accepted = 0;
        var invited = 0;
        var suspended = 0;

        foreach (var member in members)
        {
            if (member == null) continue;

            // An invitation is not yet a member. Counting it as one overstates the team, and the states
            // are deliberately not collapsed here: "3 (+2)" answers a question "5" hides.
            if (member.State == MembershipState.Member) accepted++;
            else invited++;

            if (member.SuspendedAt != null) suspended++;
        }

        return new TeamMemberCount(accepted, invited, suspended);
    }
}
