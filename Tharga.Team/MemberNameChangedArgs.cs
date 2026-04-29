namespace Tharga.Team;

/// <summary>
/// Arguments for the <c>OnMemberNameChanged</c> callback fired by <c>TeamComponent</c> after
/// a successful inline edit of <see cref="ITeamMember.Name"/>.
/// </summary>
/// <param name="TeamKey">The team that owns the member.</param>
/// <param name="MemberKey">The user key of the affected member.</param>
/// <param name="OldName">The previous member display name (may be null when no override was set).</param>
/// <param name="NewName">The new member display name (null when the override was cleared).</param>
public sealed record MemberNameChangedArgs(string TeamKey, string MemberKey, string OldName, string NewName);
