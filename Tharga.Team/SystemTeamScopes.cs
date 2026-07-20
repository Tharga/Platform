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

    /// <summary>
    /// Authorizes enumerating <b>any</b> team via <c>ITeamService.GetAllTeamsAsync</c>, regardless of
    /// membership — the discovery path for oversight roles (support, administration).
    /// </summary>
    /// <remarks>
    /// Discovery only. Holding this grants no access <i>inside</i> a team: selecting a team the caller
    /// is not a member of still yields only the scopes that team has consented to, and none if it has
    /// consented to nothing. Contrast with the in-team <see cref="TeamScopes.Read"/>, which authorizes
    /// reading the caller's own team.
    /// </remarks>
    public const string Read = "teams:read";
}
