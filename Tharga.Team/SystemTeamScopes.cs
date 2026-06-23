namespace Tharga.Team;

/// <summary>
/// System-level (cross-team) scope constants for team operations. Unlike the in-team <see cref="TeamScopes"/>
/// (which authorize only the caller's own team), these authorize across any team and are granted to system
/// API keys or privileged roles. The toolkit defines them; the consumer applies them as they see fit
/// (e.g. <c>o.ConfigureSystemRoles</c> mapping a role to the scope, or a system API key's scope list).
/// </summary>
public static class SystemTeamScopes
{
    /// <summary>
    /// Authorizes deleting <b>any</b> team, regardless of membership and regardless of the
    /// <c>AllowTeamCreation</c> self-service option. The unconditional, cross-team delete path.
    /// </summary>
    public const string Delete = "teams:delete";
}
