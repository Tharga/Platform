namespace Tharga.Team;

/// <summary>
/// What an invite code resolves to. Everything an invitation screen needs, and nothing more.
/// </summary>
/// <param name="TeamKey">The team the invitation is for.</param>
/// <param name="TeamName">Its display name.</param>
/// <param name="EMail">The address the invitation was sent to.</param>
/// <param name="AlreadyMember">
/// True when the caller is already an accepted member of this team, so the screen can say so instead of
/// offering to join twice.
/// </param>
public sealed record TeamInvitation(string TeamKey, string TeamName, string EMail, bool AlreadyMember);

/// <summary>
/// Resolves an invite code to the invitation it names — <b>the interface an invitation screen should
/// inject</b>.
/// </summary>
/// <remarks>
/// <b>Authorized by the code, not by a scope</b>, which is why this is its own interface rather than a
/// member of a <c>[RequireScope]</c> one. An invitee is not yet a member and holds nothing for the team
/// they are being invited to; requiring a scope would make an invitation impossible to accept. The rule
/// is that a first-level call is <i>checked</i>, not that it is checked by a scope.
/// <para>
/// It replaces the pattern it supersedes: reading an arbitrary team by naming its key and matching the
/// code against the roster in memory. That returned the whole team — every member, access level and
/// membership state — to someone who had only been sent a link, and it is why the invitation screen was
/// the one read that could not simply move onto the gated path.
/// </para>
/// <para>
/// Returns null for a code that is malformed, unknown, or no longer outstanding. The three are
/// deliberately indistinguishable to the caller: telling an unauthenticated visitor which of them
/// applies would confirm whether a team exists.
/// </para>
/// </remarks>
public interface ITeamInvitationService
{
    /// <summary>The invitation this code names, or null.</summary>
    Task<TeamInvitation> GetInvitationAsync(string inviteCode);
}
