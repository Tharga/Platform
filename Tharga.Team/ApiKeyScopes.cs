namespace Tharga.Team;

/// <summary>
/// Scope constants for API key management.
/// </summary>
public static class ApiKeyScopes
{
    public const string Manage = "apikey:manage";

    /// <summary>Scope required to manage system-level API keys (keys not bound to a team).</summary>
    public const string SystemManage = "apikey:system-manage";
}
