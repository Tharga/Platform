namespace Tharga.Team;

/// <summary>
/// Scope constants for team and member management.
/// </summary>
public static class TeamScopes
{
    public const string Read = "team:read";
    public const string Manage = "team:manage";
    public const string MemberInvite = "member:invite";
    public const string MemberRemove = "member:remove";
    public const string MemberRole = "member:role";

    /// <summary>
    /// Umbrella scope that subsumes <see cref="MemberInvite"/>, <see cref="MemberRemove"/> and
    /// <see cref="MemberRole"/> — a principal holding it is authorized for all member-management
    /// operations. The granular scopes remain available for fine-grained cases.
    /// </summary>
    public const string MemberManage = "member:manage";
}
