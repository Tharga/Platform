namespace Tharga.Team;

/// <summary>
/// Scope constants for team and member management.
/// </summary>
public static class TeamScopes
{
    public const string Read = "team:read";
    public const string Manage = "team:manage";
    /// <summary>
    /// Authorizes all member-management operations: inviting, removing, and changing members' access
    /// level, roles, and scope overrides.
    /// </summary>
    public const string MemberManage = "member:manage";
}
