namespace Tharga.Team.Blazor.Features.Team;

/// <summary>
/// Decides which team an <i>automatic</i> resolution should land on when the current selection is
/// missing or no longer valid.
/// </summary>
/// <remarks>
/// The security-critical distinction lives here. A team the caller <i>chose</i> — this session's
/// selection, or the one remembered from a previous visit — is honoured as long as they may still see
/// it, whether or not they are a member. But when there is no such choice, the <b>fallback comes from
/// the caller's own memberships only</b>: defaulting out of the widened <c>teams:read</c> set would
/// park an oversight caller inside an arbitrary tenant they never picked, with a page refresh making it
/// look deliberate.
/// </remarks>
internal static class TeamSelectionResolver
{
    public static ITeam Resolve(string currentTeamKey, string rememberedTeamKey, IReadOnlyList<ITeam> visibleTeams, IReadOnlyList<ITeam> ownTeams)
    {
        return Visible(currentTeamKey, visibleTeams)
               ?? Visible(rememberedTeamKey, visibleTeams)
               ?? ownTeams?.FirstOrDefault();
    }

    private static ITeam Visible(string teamKey, IReadOnlyList<ITeam> visibleTeams)
    {
        return string.IsNullOrEmpty(teamKey)
            ? null
            : visibleTeams?.FirstOrDefault(x => x.Key == teamKey);
    }
}
