namespace Tharga.Team;

public enum AccessLevel
{
    Owner,
    Administrator,
    User,
    Viewer,

    /// <summary>
    /// Grants no inherited base scopes. A principal at this level has exactly the scopes from
    /// its assigned roles and <c>ScopeOverrides</c> — nothing from the access-level tier itself,
    /// and it is exempt from the "Owner/Administrator get all registered scopes" rule. Use for
    /// least-privilege machine keys that should carry only their explicit grants.
    /// </summary>
    Custom
}
