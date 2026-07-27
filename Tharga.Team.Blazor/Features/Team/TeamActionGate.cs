namespace Tharga.Team.Blazor.Features.Team;

/// <summary>
/// Visibility and enablement gates for the per-team action buttons rendered by
/// <c>TeamComponent</c>.
/// </summary>
/// <remarks>
/// The <c>team:manage</c> scope is emitted by <c>TeamServerClaimsTransformation</c> for the
/// currently-selected team only, so holding it authorizes actions on that team and no other.
/// Gating every team card on the bare scope flag offers buttons the server then rejects with
/// <see cref="UnauthorizedAccessException"/> (Tharga/Team#125).
/// </remarks>
internal static class TeamActionGate
{
    /// <summary>
    /// Whether the caller may manage <paramref name="teamKey"/>: the manage scope is held and that
    /// team is the selected one the scope was issued for.
    /// </summary>
    public static bool CanManage(bool hasManageScope, string selectedTeamKey, string teamKey)
        => hasManageScope && IsSelected(selectedTeamKey, teamKey);

    /// <summary>
    /// Whether <paramref name="teamKey"/> is the currently selected team. Every per-team action is
    /// confined to it: the scopes are issued for the selected team, and an action offered on another
    /// team card is one the caller cannot carry out there.
    /// </summary>
    private static bool IsSelected(string selectedTeamKey, string teamKey)
    {
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
    /// (Tharga/Team#134).
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
    /// Whether the Leave action should be visible: on the selected team, where the caller is a member
    /// but not the owner. Non-members have nothing to leave and the owner must transfer ownership
    /// instead; leaving elsewhere is refused by the service, which requires the member-manage scope on
    /// the team being left.
    /// </summary>
    public static bool CanLeave(bool isMember, bool isOwner, string selectedTeamKey, string teamKey)
        => isMember && !isOwner && IsSelected(selectedTeamKey, teamKey);

    /// <summary>
    /// Whether the Transfer ownership action should be visible: on the selected team, where the caller
    /// owns it and there is somebody to hand it to.
    /// </summary>
    public static bool CanTransferOwnership(bool isOwner, bool hasOtherMembers, string selectedTeamKey, string teamKey)
        => isOwner && hasOtherMembers && IsSelected(selectedTeamKey, teamKey);

    /// <summary>
    /// Whether the consent selector should be editable: manage rights on this team and administrator
    /// level on it. It stays visible either way so the consented level can be read without being
    /// changeable. Access level alone is not sufficient — <c>SetTeamConsentAsync</c> is enforced on
    /// <c>team:manage</c>, which is issued for the selected team only, so gating on level alone offered
    /// an edit the service then rejected on every other team (Tharga/Team#140).
    /// </summary>
    public static bool CanEditConsent(bool hasManageScope, string selectedTeamKey, string teamKey, bool isAdministrator)
        => CanManage(hasManageScope, selectedTeamKey, teamKey) && isAdministrator;
}
