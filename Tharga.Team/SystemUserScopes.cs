namespace Tharga.Team;

/// <summary>
/// System-level scope constants for user administration. Like <see cref="SystemTeamScopes"/>, these
/// authorize cross-team operations and are granted to privileged roles or system API keys. The toolkit
/// defines them; the consumer applies them as they see fit (e.g. <c>o.ConfigureSystemRoles</c> mapping
/// a role to the scope).
/// </summary>
public static class SystemUserScopes
{
    /// <summary>
    /// Authorizes user administration via <see cref="IUserManagementService"/>: verifying users against
    /// the external directory, listing directory-only users, and deleting users (from the local store
    /// and, on explicit opt-in, from the directory).
    /// </summary>
    public const string Manage = "users:manage";
}
