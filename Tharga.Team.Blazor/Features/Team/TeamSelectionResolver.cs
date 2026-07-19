namespace Tharga.Team.Blazor.Features.Team;

/// <summary>
/// Decides which team an <i>automatic</i> resolution should land on when the current selection is
/// missing or no longer valid.
/// </summary>
/// <remarks>
/// The security-critical rule lives here: an explicitly selected team is honoured when the caller may
/// still see it, but the <b>fallback comes from the caller's own memberships only</b>. Falling back to
/// the widened <c>teams:read</c> set would silently park an oversight caller inside an arbitrary
/// tenant — with a page refresh, so it would look deliberate.
/// </remarks>
internal static class TeamSelectionResolver
{
    public static ITeam Resolve(string selectedTeamKey, IReadOnlyList<ITeam> visibleTeams, IReadOnlyList<ITeam> ownTeams)
    {
        var explicitlySelected = string.IsNullOrEmpty(selectedTeamKey)
            ? null
            : visibleTeams?.FirstOrDefault(x => x.Key == selectedTeamKey);

        return explicitlySelected ?? ownTeams?.FirstOrDefault();
    }
}
