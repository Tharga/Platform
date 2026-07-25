namespace Tharga.Team.Blazor.Features.Team;

/// <summary>
/// Visibility and enablement gates for the per-team action buttons rendered by
/// <c>TeamComponent</c>.
/// </summary>
/// <remarks>
/// The <c>team:manage</c> scope is emitted by <c>TeamServerClaimsTransformation</c> for the
/// currently-selected team only, so holding it authorizes actions on that team and no other.
/// Gating every team card on the bare scope flag offers buttons the server then rejects with
/// <see cref="UnauthorizedAccessException"/> (Tharga/Platform#125).
/// </remarks>
internal static class TeamActionGate
{
    /// <summary>
    /// Whether the caller may manage <paramref name="teamKey"/>: the manage scope is held and that
    /// team is the selected one the scope was issued for.
    /// </summary>
    public static bool CanManage(bool hasManageScope, string selectedTeamKey, string teamKey)
    {
        if (!hasManageScope) return false;
        if (string.IsNullOrEmpty(selectedTeamKey) || string.IsNullOrEmpty(teamKey)) return false;
        return string.Equals(selectedTeamKey, teamKey, StringComparison.Ordinal);
    }

    /// <summary>Whether the Rename action should be visible.</summary>
    public static bool CanRename(bool hasManageScope, string selectedTeamKey, string teamKey)
        => CanManage(hasManageScope, selectedTeamKey, teamKey);

    /// <summary>
    /// Whether member-management actions (e.g. Invite User) should be visible: the member-manage scope is
    /// held and that team is the selected one it was issued for. Like the manage scope, member:manage is
    /// emitted only for the selected team, so a global flag would offer the action on every card
    /// (Tharga/Platform#134).
    /// </summary>
    public static bool CanManageMembers(bool hasMemberManageScope, string selectedTeamKey, string teamKey)
        => CanManage(hasMemberManageScope, selectedTeamKey, teamKey);

    /// <summary>
    /// Whether the Delete action should be visible: manage rights on this team, host-enabled team
    /// creation, and team ownership.
    /// </summary>
    public static bool CanDelete(bool hasManageScope, string selectedTeamKey, string teamKey, bool allowTeamCreation, bool isOwner)
        => CanManage(hasManageScope, selectedTeamKey, teamKey) && allowTeamCreation && isOwner;

    /// <summary>
    /// Whether the Leave action should be visible. Non-members have nothing to leave; the owner
    /// must transfer ownership instead.
    /// </summary>
    public static bool CanLeave(bool isMember, bool isOwner) => isMember && !isOwner;

    /// <summary>
    /// Whether the consent selector should be editable: manage rights on this team and administrator
    /// level on it. It stays visible either way so the consented level can be read without being
    /// changeable. Access level alone is not sufficient — <c>SetTeamConsentAsync</c> is enforced on
    /// <c>team:manage</c>, which is issued for the selected team only, so gating on level alone offered
    /// an edit the service then rejected on every other team (Tharga/Platform#140).
    /// </summary>
    public static bool CanEditConsent(bool hasManageScope, string selectedTeamKey, string teamKey, bool isAdministrator)
        => CanManage(hasManageScope, selectedTeamKey, teamKey) && isAdministrator;
}
