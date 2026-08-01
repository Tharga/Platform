namespace Tharga.Team;

/// <summary>
/// Decides whether a team is in the ownerless state that <see cref="SystemTeamScopes.AssignOwner"/>
/// repairs, and who may be given the role.
/// </summary>
/// <remarks>
/// Pure and static so the rules are testable without a store. They are the whole safety argument for the
/// operation, so they should not live inside a service method where only an integration test can reach
/// them.
/// <para>
/// The platform protects against ownerless teams on one path and creates them freely on another:
/// <c>SetMemberRoleAsync</c> refuses to grant or revoke <c>Owner</c>, while
/// <c>RemoveUserFromAllTeamsAsync</c> removes the owner along with everyone else. Deleting a user
/// therefore produces exactly the state the first rule exists to prevent, and nothing could undo it —
/// <c>TransferOwnershipAsync</c> requires the caller to be the current owner, so with the owner gone
/// there was no path back.
/// </para>
/// </remarks>
public static class TeamOwnership
{
    /// <summary>
    /// Whether the team currently has no member at <see cref="AccessLevel.Owner"/> — the only state in
    /// which assigning an owner is a repair rather than a takeover.
    /// </summary>
    /// <remarks>
    /// A null or empty roster counts as ownerless, but see <see cref="CanAssign"/>: there is then nobody
    /// to promote, so the operation still refuses.
    /// </remarks>
    public static bool IsOwnerless(IEnumerable<ITeamMember> members)
        => members?.Any(m => m != null && m.AccessLevel == AccessLevel.Owner) != true;

    /// <summary>
    /// Whether <paramref name="candidateUserKey"/> may be made owner of this team.
    /// </summary>
    /// <remarks>
    /// Two conditions, and both matter. The team must currently be <b>ownerless</b>, so nobody is being
    /// escalated past. The candidate must be an <b>existing member</b>, which keeps this a repair rather
    /// than a way to inject an outsider into a team the caller does not belong to.
    /// </remarks>
    public static bool CanAssign(IEnumerable<ITeamMember> members, string candidateUserKey)
    {
        if (string.IsNullOrEmpty(candidateUserKey)) return false;

        var roster = members?.Where(m => m != null).ToArray() ?? [];
        if (!IsOwnerless(roster)) return false;

        return roster.Any(m => m.Key == candidateUserKey);
    }
}
